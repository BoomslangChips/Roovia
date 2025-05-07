// chartInterop.js - Safe JavaScript interop for Chart.js

// Create namespace for our chart functions
window.ChartInterop = {
    // Initialize the storage chart
    initStorageChart: function (id, categories, storageData, fileCountData) {
        // Make sure Chart.js is loaded
        if (typeof Chart === 'undefined') {
            console.error('Chart.js is not loaded');
            return false;
        }

        // Background colors for the charts
        const backgroundColors = [
            'rgba(54, 162, 235, 0.6)',
            'rgba(255, 99, 132, 0.6)',
            'rgba(255, 206, 86, 0.6)',
            'rgba(75, 192, 192, 0.6)',
            'rgba(153, 102, 255, 0.6)'
        ];

        const borderColors = backgroundColors.map(color => color.replace('0.6', '1'));

        // Get the canvas element
        const canvas = document.getElementById(id);
        if (!canvas) {
            console.error('Canvas element not found:', id);
            return false;
        }

        // Get the context
        const ctx = canvas.getContext('2d');
        if (!ctx) {
            console.error('Could not get 2d context from canvas');
            return false;
        }

        // Destroy existing chart if it exists
        if (window.chartInstances && window.chartInstances[id]) {
            window.chartInstances[id].destroy();
        }

        // Create storage chart
        try {
            // Initialize chart instances container if needed
            if (!window.chartInstances) {
                window.chartInstances = {};
            }

            // Create the chart
            window.chartInstances[id] = new Chart(ctx, {
                type: 'bar',
                data: {
                    labels: categories,
                    datasets: [
                        {
                            label: 'Storage Used (MB)',
                            data: storageData,
                            backgroundColor: backgroundColors,
                            borderColor: borderColors,
                            borderWidth: 1,
                            yAxisID: 'y'
                        },
                        {
                            label: 'File Count',
                            data: fileCountData,
                            type: 'line',
                            borderColor: 'rgba(255, 99, 132, 1)',
                            backgroundColor: 'rgba(255, 99, 132, 0.2)',
                            borderWidth: 2,
                            pointBackgroundColor: 'rgba(255, 99, 132, 1)',
                            fill: false,
                            yAxisID: 'y1'
                        }
                    ]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    scales: {
                        y: {
                            type: 'linear',
                            display: true,
                            position: 'left',
                            title: {
                                display: true,
                                text: 'Storage Used (MB)'
                            }
                        },
                        y1: {
                            type: 'linear',
                            display: true,
                            position: 'right',
                            title: {
                                display: true,
                                text: 'File Count'
                            },
                            grid: {
                                drawOnChartArea: false
                            }
                        }
                    },
                    plugins: {
                        legend: {
                            position: 'top',
                        },
                        tooltip: {
                            callbacks: {
                                label: function (context) {
                                    let label = context.dataset.label || '';
                                    if (label) {
                                        label += ': ';
                                    }
                                    if (context.dataset.yAxisID === 'y') {
                                        label += context.parsed.y.toFixed(2) + ' MB';
                                    } else {
                                        label += context.parsed.y;
                                    }
                                    return label;
                                }
                            }
                        }
                    }
                }
            });

            return true;
        } catch (error) {
            console.error('Error creating storage chart:', error);
            return false;
        }
    },

    // Update the storage chart
    updateStorageChart: function (id, categories, storageData, fileCountData) {
        try {
            if (!window.chartInstances || !window.chartInstances[id]) {
                console.warn('Chart not found, initializing instead');
                return this.initStorageChart(id, categories, storageData, fileCountData);
            }

            const chart = window.chartInstances[id];
            chart.data.labels = categories;
            chart.data.datasets[0].data = storageData;
            chart.data.datasets[1].data = fileCountData;
            chart.update();

            return true;
        } catch (error) {
            console.error('Error updating storage chart:', error);
            return false;
        }
    },

    // Initialize the file type chart (doughnut chart)
    initFileTypeChart: function (id, fileTypes, countData) {
        // Make sure Chart.js is loaded
        if (typeof Chart === 'undefined') {
            console.error('Chart.js is not loaded');
            return false;
        }

        // Define colors for each file type
        const backgroundColors = [
            'rgba(54, 162, 235, 0.6)',  // Image (blue)
            'rgba(255, 99, 132, 0.6)',  // PDF (red)
            'rgba(75, 192, 192, 0.6)',  // Document (green)
            'rgba(255, 206, 86, 0.6)',  // Spreadsheet (yellow)
            'rgba(153, 102, 255, 0.6)', // Video (purple)
            'rgba(255, 159, 64, 0.6)',  // Audio (orange)
            'rgba(201, 203, 207, 0.6)'  // Other (gray)
        ];

        const borderColors = backgroundColors.map(color => color.replace('0.6', '1'));

        // Get the canvas element
        const canvas = document.getElementById(id);
        if (!canvas) {
            console.error('Canvas element not found:', id);
            return false;
        }

        // Get the context
        const ctx = canvas.getContext('2d');
        if (!ctx) {
            console.error('Could not get 2d context from canvas');
            return false;
        }

        // Destroy existing chart if it exists
        if (window.chartInstances && window.chartInstances[id]) {
            window.chartInstances[id].destroy();
        }

        // Create file type chart
        try {
            // Initialize chart instances container if needed
            if (!window.chartInstances) {
                window.chartInstances = {};
            }

            // Create the chart
            window.chartInstances[id] = new Chart(ctx, {
                type: 'doughnut',
                data: {
                    labels: fileTypes,
                    datasets: [
                        {
                            data: countData,
                            backgroundColor: backgroundColors.slice(0, fileTypes.length),
                            borderColor: borderColors.slice(0, fileTypes.length),
                            borderWidth: 1
                        }
                    ]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            position: 'right',
                        },
                        tooltip: {
                            callbacks: {
                                label: function (context) {
                                    const label = context.label || '';
                                    const value = context.raw;
                                    const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                    const percentage = Math.round((value / total) * 100);
                                    return `${label}: ${value} (${percentage}%)`;
                                }
                            }
                        }
                    }
                }
            });

            return true;
        } catch (error) {
            console.error('Error creating file type chart:', error);
            return false;
        }
    },

    // Update the file type chart
    updateFileTypeChart: function (id, fileTypes, countData) {
        try {
            if (!window.chartInstances || !window.chartInstances[id]) {
                console.warn('Chart not found, initializing instead');
                return this.initFileTypeChart(id, fileTypes, countData);
            }

            const chart = window.chartInstances[id];
            chart.data.labels = fileTypes;
            chart.data.datasets[0].data = countData;
            chart.update();

            return true;
        } catch (error) {
            console.error('Error updating file type chart:', error);
            return false;
        }
    },

    // Load Chart.js if not already loaded
    loadChartJs: function () {
        return new Promise((resolve, reject) => {
            if (window.Chart) {
                resolve(true);
                return;
            }

            const script = document.createElement('script');
            script.src = 'https://cdn.jsdelivr.net/npm/chart.js@3.9.1/dist/chart.min.js';
            script.integrity = 'sha256-+k+j9L0jAtMkNrynWTdOTfTZrKtohhXHpdZWBENjbDI=';
            script.crossOrigin = 'anonymous';
            script.onload = () => resolve(true);
            script.onerror = () => reject(new Error('Failed to load Chart.js'));
            document.head.appendChild(script);
        });
    }
};