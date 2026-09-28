// roovia-connection.js
// Starts Blazor with the live-connection settings from appsettings.json (Connectivity:Client)
// and drives the reconnect UI (#components-reconnect-modal in App.razor).
//
// blazor.web.js toggles these classes on the modal:
//   components-reconnect-show      connection lost, retrying
//   components-reconnect-hide      connection restored
//   components-reconnect-failed    retries used up
//   components-reconnect-rejected  server no longer has this session (restart / expired)

(function () {
    const script = document.currentScript;
    const options = parseOptions(script?.dataset.options);
    const modal = document.getElementById('components-reconnect-modal');

    const STATE = {
        retrying: 'components-reconnect-show',
        failed: 'components-reconnect-failed',
        rejected: 'components-reconnect-rejected'
    };

    let manualReconnect = null;

    function parseOptions(json) {
        try { return JSON.parse(json || '{}'); } catch { return {}; }
    }

    function hasState(state) {
        return modal?.classList.contains(state) ?? false;
    }

    function buildCircuitOptions() {
        const intervals = Array.isArray(options.retryIntervalsMilliseconds)
            ? options.retryIntervalsMilliseconds.filter(ms => Number.isFinite(ms) && ms >= 0)
            : [];

        const reconnectionOptions = {};
        if (options.maxRetries > 0) {
            reconnectionOptions.maxRetries = options.maxRetries;
        }
        if (intervals.length > 0) {
            // The last interval repeats for every remaining attempt
            reconnectionOptions.retryIntervalMilliseconds = attempt => intervals[Math.min(attempt, intervals.length - 1)];
        }

        return {
            reconnectionOptions,
            configureSignalR: builder => {
                if (options.serverTimeoutMilliseconds > 0) {
                    builder.withServerTimeout(options.serverTimeoutMilliseconds);
                }
                if (options.keepAliveIntervalMilliseconds > 0) {
                    builder.withKeepAliveInterval(options.keepAliveIntervalMilliseconds);
                }
            }
        };
    }

    function reload() {
        location.reload();
    }

    // Used once Blazor's own retry loop has given up
    async function reconnectNow() {
        if (manualReconnect) return manualReconnect;

        manualReconnect = (async () => {
            try {
                const resumed = await Blazor.reconnect();
                if (!resumed) {
                    // Server is reachable but has discarded this circuit
                    reload();
                }
            } catch {
                // Still offline; stay in the failed state and wait for the next trigger
            } finally {
                manualReconnect = null;
            }
        })();

        return manualReconnect;
    }

    // Skip the remaining back-off wait. Blazor's retry loop starts the next attempt
    // immediately when the page reports itself visible.
    function retrySooner() {
        if (hasState(STATE.retrying) && document.visibilityState === 'visible') {
            document.dispatchEvent(new Event('visibilitychange'));
        }
    }

    function onNetworkOrFocusRestored() {
        if (hasState(STATE.failed)) {
            reconnectNow();
        } else {
            retrySooner();
        }
    }

    function watchModal() {
        if (!modal) return;

        new MutationObserver(() => {
            if (hasState(STATE.rejected)) {
                reload();
            }
        }).observe(modal, { attributes: true, attributeFilter: ['class'] });

        modal.addEventListener('click', event => {
            const action = event.target.closest('[data-reconnect-action]')?.dataset.reconnectAction;
            if (action === 'retry') {
                reconnectNow();
            } else if (action === 'reload') {
                reload();
            }
        });

        window.addEventListener('online', onNetworkOrFocusRestored);
        document.addEventListener('visibilitychange', () => {
            if (document.visibilityState === 'visible' && hasState(STATE.failed)) {
                reconnectNow();
            }
        });
    }

    watchModal();

    Blazor.start({ circuit: buildCircuitOptions() }).catch(error => {
        // The first connection never came up (e.g. WebSockets blocked): offer a reload instead of a blank page
        console.error('Roovia: unable to start the live connection.', error);
        modal?.classList.remove(STATE.retrying);
        modal?.classList.add(STATE.failed);
        modal?.querySelector('[data-reconnect-action="retry"]')?.setAttribute('data-reconnect-action', 'reload');
    });
})();
