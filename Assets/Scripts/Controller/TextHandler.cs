using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class TextHandler
{
    private Dictionary<string, Func<string>> variableMap;

    public TextHandler()
    {
        variableMap = new Dictionary<string, Func<string>>();
        InitializeVariables();
    }

    private void InitializeVariables()
    {
        // Register all available text variables here
        variableMap["LastFollowerThatDied"] = () =>
        {
            if (Controller.Instance?.CanonGameState?.LastFollowerThatDied == null)
                return "None";
            return Controller.Instance.CanonGameState.LastFollowerThatDied.GetName();
        };

        // Add more variables here as needed
        // Example:
        // variableMap["CurrentPlayerName"] = () => Controller.Instance.CurrentPlayer?.GetName() ?? "Unknown";
    }

    /// <summary>
    /// Processes a string and replaces variables in the format [VariableName] with their actual values.
    /// </summary>
    /// <param name="text">The input string containing variables in brackets</param>
    /// <returns>The processed string with variables replaced</returns>
    public string ProcessText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Match patterns like [VariableName]
        return Regex.Replace(text, @"\[(\w+)\]", match =>
        {
            string variableName = match.Groups[1].Value;
            
            if (variableMap.TryGetValue(variableName, out Func<string> getValue))
            {
                try
                {
                    return getValue();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Error processing variable [{variableName}]: {ex.Message}");
                    return $"[{variableName}]"; // Return original if error occurs
                }
            }
            
            // Variable not found, return original
            UnityEngine.Debug.LogWarning($"Text variable [{variableName}] not found in TextHandler");
            return match.Value;
        });
    }

    /// <summary>
    /// Registers a new variable that can be used in text processing.
    /// </summary>
    /// <param name="variableName">The name of the variable (without brackets)</param>
    /// <param name="getValue">Function that returns the value to replace the variable with</param>
    public void RegisterVariable(string variableName, Func<string> getValue)
    {
        variableMap[variableName] = getValue;
    }
}

