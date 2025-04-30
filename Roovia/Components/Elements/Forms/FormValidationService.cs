using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Roovia.Components.Elements.Forms
{
    /// <summary>
    /// Service for handling form validation in Blazor applications.
    /// Provides utilities for field validation, error collection and custom validators.
    /// </summary>
    public class FormValidationService
    {
        /// <summary>
        /// Validates a single property of an object using DataAnnotations.
        /// </summary>
        /// <typeparam name="T">The type of the object to validate.</typeparam>
        /// <param name="obj">The object to validate.</param>
        /// <param name="propertyName">The name of the property to validate.</param>
        /// <returns>A list of validation errors for the property.</returns>
        public IEnumerable<string> ValidateProperty<T>(T obj, string propertyName)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException(nameof(propertyName));

            var propertyInfo = typeof(T).GetProperty(propertyName);
            if (propertyInfo == null)
                throw new ArgumentException($"Property {propertyName} not found on type {typeof(T).Name}");

            var value = propertyInfo.GetValue(obj);
            var validationContext = new ValidationContext(obj) { MemberName = propertyName };
            var validationResults = new List<ValidationResult>();

            Validator.TryValidateProperty(value, validationContext, validationResults);

            return validationResults.Select(r => r.ErrorMessage);
        }

        /// <summary>
        /// Validates an entire object using DataAnnotations.
        /// </summary>
        /// <typeparam name="T">The type of the object to validate.</typeparam>
        /// <param name="obj">The object to validate.</param>
        /// <returns>A dictionary of property names and their validation errors.</returns>
        public Dictionary<string, List<string>> ValidateObject<T>(T obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(obj);
            var isValid = Validator.TryValidateObject(obj, validationContext, validationResults, true);

            var errors = new Dictionary<string, List<string>>();

            if (!isValid)
            {
                foreach (var validationResult in validationResults)
                {
                    foreach (var memberName in validationResult.MemberNames)
                    {
                        if (!errors.ContainsKey(memberName))
                        {
                            errors[memberName] = new List<string>();
                        }

                        errors[memberName].Add(validationResult.ErrorMessage);
                    }
                }
            }

            return errors;
        }

        /// <summary>
        /// Gets validation errors from an EditContext for a specific property.
        /// </summary>
        /// <param name="editContext">The edit context.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>A list of validation errors for the property.</returns>
        public IEnumerable<string> GetFieldErrors(EditContext editContext, string propertyName)
        {
            if (editContext == null)
                throw new ArgumentNullException(nameof(editContext));

            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException(nameof(propertyName));

            var fieldIdentifier = new FieldIdentifier(editContext.Model, propertyName);
            return editContext.GetValidationMessages(fieldIdentifier);
        }

        /// <summary>
        /// Creates a field identifier from an expression.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="accessor">An expression to access a property.</param>
        /// <returns>A field identifier for the property.</returns>
        public FieldIdentifier CreateFieldIdentifier<TValue>(Expression<Func<TValue>> accessor)
        {
            return FieldIdentifier.Create(accessor);
        }

        /// <summary>
        /// Adds a custom validation error to an EditContext.
        /// </summary>
        /// <param name="editContext">The edit context.</param>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="errorMessage">The error message.</param>
        public void AddValidationError(EditContext editContext, string fieldName, string errorMessage)
        {
            if (editContext == null)
                throw new ArgumentNullException(nameof(editContext));

            if (string.IsNullOrEmpty(fieldName))
                throw new ArgumentNullException(nameof(fieldName));

            if (string.IsNullOrEmpty(errorMessage))
                throw new ArgumentNullException(nameof(errorMessage));

            var fieldIdentifier = new FieldIdentifier(editContext.Model, fieldName);
            var validationMessageStore = new ValidationMessageStore(editContext);
            validationMessageStore.Add(fieldIdentifier, errorMessage);
            editContext.NotifyValidationStateChanged();
        }

        /// <summary>
        /// Adds custom validation errors to an EditContext.
        /// </summary>
        /// <param name="editContext">The edit context.</param>
        /// <param name="errors">A dictionary of field names and error messages.</param>
        public void AddValidationErrors(EditContext editContext, Dictionary<string, List<string>> errors)
        {
            if (editContext == null)
                throw new ArgumentNullException(nameof(editContext));

            if (errors == null || !errors.Any())
                return;

            var validationMessageStore = new ValidationMessageStore(editContext);

            foreach (var error in errors)
            {
                var fieldIdentifier = new FieldIdentifier(editContext.Model, error.Key);
                foreach (var message in error.Value)
                {
                    validationMessageStore.Add(fieldIdentifier, message);
                }
            }

            editContext.NotifyValidationStateChanged();
        }

        /// <summary>
        /// Clears all validation errors from an EditContext.
        /// </summary>
        /// <param name="editContext">The edit context.</param>
        public void ClearValidationErrors(EditContext editContext)
        {
            if (editContext == null)
                throw new ArgumentNullException(nameof(editContext));

            var validationMessageStore = new ValidationMessageStore(editContext);
            validationMessageStore.Clear();
            editContext.NotifyValidationStateChanged();
        }

        /// <summary>
        /// Registers a custom validator to be used with an EditContext.
        /// </summary>
        /// <param name="editContext">The edit context.</param>
        /// <param name="validator">The custom validator function.</param>
        public void RegisterCustomValidator(EditContext editContext, Func<Dictionary<string, List<string>>> validator)
        {
            if (editContext == null)
                throw new ArgumentNullException(nameof(editContext));

            if (validator == null)
                throw new ArgumentNullException(nameof(validator));

            // Create a validation message store for the edit context
            var messageStore = new ValidationMessageStore(editContext);

            // Subscribe to the validation requested event
            editContext.OnValidationRequested += (sender, eventArgs) =>
            {
                messageStore.Clear();
                var errors = validator();
                foreach (var error in errors)
                {
                    var fieldIdentifier = new FieldIdentifier(editContext.Model, error.Key);
                    foreach (var message in error.Value)
                    {
                        messageStore.Add(fieldIdentifier, message);
                    }
                }
                editContext.NotifyValidationStateChanged();
            };

            // Subscribe to the field changed event
            editContext.OnFieldChanged += (sender, eventArgs) =>
            {
                var fieldIdentifier = eventArgs.FieldIdentifier;
                var propertyName = fieldIdentifier.FieldName;

                // Clear messages for this field
                messageStore.Clear(fieldIdentifier);

                // Re-validate just this field
                var errors = validator();
                if (errors.TryGetValue(propertyName, out var fieldErrors))
                {
                    foreach (var error in fieldErrors)
                    {
                        messageStore.Add(fieldIdentifier, error);
                    }
                }

                editContext.NotifyValidationStateChanged();
            };
        }
    }

}