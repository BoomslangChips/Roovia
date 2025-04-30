using System.Collections.Generic;

namespace Roovia.Forms
{
    /// <summary>
    /// Enum defining when validation should be triggered
    /// </summary>
    public enum ValidationTriggerType
    {
        OnSubmit,
        OnChange,
        OnBlur
    }

    /// <summary>
    /// Enum defining the validation method to use
    /// </summary>
    public enum ValidationModeType
    {
        DataAnnotations,
        FluentValidation,
        Custom
    }

    /// <summary>
    /// Enum for tracking form state
    /// </summary>
    public enum FormStateType
    {
        Idle,
        Submitting,
        Success,
        Error
    }

    /// <summary>
    /// Context class for passing form information to child components
    /// </summary>
    public class FormContext
    {
        public Microsoft.AspNetCore.Components.Forms.EditContext EditContext { get; set; }
        public ValidationTriggerType ValidationTrigger { get; set; }
    }

    /// <summary>
    /// Arguments for form submission events
    /// </summary>
    public class FormSubmitEventArgs<T> where T : class
    {
        public T Model { get; set; }
        public bool IsValid { get; set; }
        public Dictionary<string, List<string>> ValidationErrors { get; set; } = new Dictionary<string, List<string>>();
    }

    /// <summary>
    /// Arguments for form state change events
    /// </summary>
    public class FormStateChangedEventArgs
    {
        public bool IsValid { get; set; }
        public FormStateType State { get; set; }
    }
}