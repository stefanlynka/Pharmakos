using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class RunContent
{
    static Dictionary<string, Type> typesByName;

    public static Card CreateCard(string typeName)
    {
        Type type = FindType(typeName);
        if (type == null || !typeof(Card).IsAssignableFrom(type))
        {
            if (!string.IsNullOrEmpty(typeName))
                Debug.LogError("Run save: unknown card " + typeName);
            return null;
        }

        return (Card)Activator.CreateInstance(type);
    }

    public static Ritual CreateRitual(string typeName)
    {
        if (string.IsNullOrEmpty(typeName)) return null;

        Type type = FindType(typeName);
        if (type == null || !typeof(Ritual).IsAssignableFrom(type))
        {
            Debug.LogError("Run save: unknown ritual " + typeName);
            return null;
        }

        return (Ritual)Activator.CreateInstance(type);
    }

    public static Trinket CreateTrinket(string typeName)
    {
        if (string.IsNullOrEmpty(typeName)) return null;

        Type type = FindType(typeName);
        if (type == null || !typeof(Trinket).IsAssignableFrom(type))
        {
            Debug.LogError("Run save: unknown trinket " + typeName);
            return null;
        }

        return (Trinket)Activator.CreateInstance(type);
    }

    public static Follower CreateFollower(string typeName)
    {
        Card card = CreateCard(typeName);
        if (card is Follower follower)
            return follower;

        if (!string.IsNullOrEmpty(typeName))
            Debug.LogError("Run save: " + typeName + " is not a follower.");
        return null;
    }

    static Type FindType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName)) return null;
        EnsureTypes();
        typesByName.TryGetValue(typeName, out Type type);
        return type;
    }

    static void EnsureTypes()
    {
        if (typesByName != null) return;

        typesByName = new Dictionary<string, Type>();
        Type[] types;
        try
        {
            types = typeof(Card).Assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            types = exception.Types;
        }

        if (types == null) return;

        foreach (Type type in types)
        {
            if (type == null || type.IsAbstract || !type.IsClass) continue;
            if (!typesByName.ContainsKey(type.Name) || string.IsNullOrEmpty(type.Namespace))
                typesByName[type.Name] = type;
        }
    }
}
