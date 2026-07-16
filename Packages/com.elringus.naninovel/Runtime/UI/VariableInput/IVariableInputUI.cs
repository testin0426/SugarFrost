namespace Naninovel.UI
{
    /// <summary>
    /// Implementation is able to present an input UI to set a custom state variable
    /// when requested by <see cref="Commands.InputCustomVariable"/> command.
    /// </summary>
    public interface IVariableInputUI : IManagedUI
    {
        /// <summary>
        /// Optional preferences of the input UI behaviour.
        /// </summary>
        public struct Options
        {
            /// <summary>
            /// Summary text to show in the UI. Empty by default.
            /// </summary>
            public LocalizableText Summary;
            /// <summary>
            /// A predefined value to set for the input field.
            /// Pulls from the assigned variable by default.
            /// </summary>
            public LocalizableText PredefinedValue;
            /// <summary>
            /// Type of the variable value.
            /// Pulls from the assigned variable by default.
            /// </summary>
            public CustomVariableValueType? ValueType;
            /// <summary>
            /// Identifier of a <see cref="IScriptTrack"/> on which to resume the playback
            /// once the input is submitted. Not resumed by default.
            /// </summary>
            public string ResumeTrackId;
        }

        /// <summary>
        /// Shows the UI to input a custom variable.
        /// </summary>
        /// <param name="variableName">Name of custom variable to assign the input value to.</param>
        /// <param name="options">Optional preferences.</param>
        void Show (string variableName, Options options = default);
    }
}
