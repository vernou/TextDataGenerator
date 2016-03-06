﻿// Copyright 2016-2016 Cédric VERNOU. All rights reserved. See LICENCE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TextDataGenerator.Core;

namespace TextDataGenerator.Factory
{
    public static class FactoryStatic
    {
        private static Dictionary<string, IFactory> factories = new Dictionary<string, IFactory>();

        public static IReadOnlyDictionary<string, IFactory> Factories { get { return factories; } }

        static FactoryStatic()
        {
            Add(new IntegerFactory());
            Add(new DoubleFactory());
            Add(new DateTimeFactory());
            Add(new RepeatFactory());
            Add(new FileLineFactory());
            Add(new TextFactory());
        }

        private static void Add(IFactory factory)
        {
            if (string.IsNullOrWhiteSpace(factory.Type))
                throw new ArgumentNullException("Factory's type is not valid.");
            factories.Add(factory.Type, factory);
        }

        public static IData CreateDataGenerator(string type, IDictionary<string, string> parameters)
        {
            if (string.IsNullOrEmpty(type))
                throw new ArgumentNullException(nameof(type));
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));
            if (factories.ContainsKey(type))
            {
                var factory = factories[type];
                factory.ResetDefaultValue();
                if (!LinkParameters(factory, parameters))
                {
                    throw new InvalidOperationException("Miss parameter of type " + type);
                }
                return factory.CreateDataGenerator();
            }
            throw new InvalidOperationException("Unknown type : " + type);
        }

        private static bool LinkParameters(IFactory factory, IDictionary<string, string> parameters)
        {
            IDictionary<string, PropertyInfo> validParameters = new Dictionary<string, PropertyInfo>();
            IDictionary<string, PropertyInfo> requiredParameters = new Dictionary<string, PropertyInfo>();
            foreach (var property in factory.GetType().GetProperties())
            {
                ParameterFactoryAttribute att = property.GetCustomAttributes(typeof(ParameterFactoryAttribute), true).FirstOrDefault() as ParameterFactoryAttribute;
                if (att == null)
                    continue;
                if (att.IsRequired)
                {
                    requiredParameters.Add(att.Name ?? property.Name, property);
                }
                else
                {
                    validParameters.Add(att.Name ?? property.Name, property);
                }
            }

            int countParameterRequiredSetted = 0;
            foreach (var parameter in parameters)
            {
                var key = parameter.Key;
                if (validParameters.ContainsKey(key))
                {
                    SetValue(factory, validParameters[key], parameter.Value);
                    continue;
                }
                if (requiredParameters.ContainsKey(key))
                {
                    SetValue(factory, requiredParameters[key], parameter.Value);
                    countParameterRequiredSetted++;
                    continue;
                }
                throw new InvalidOperationException("Bad parameter : " + key);
            }
            return requiredParameters.Count() == countParameterRequiredSetted;
        }

        private static void SetValue(Object obj, PropertyInfo property, string value)
        {
            var type = property.PropertyType;
            if (type == typeof(string))
            {
                property.SetValue(obj, value);
                return;
            }
            else if (type == typeof(int))
            {
                property.SetValue(obj, int.Parse(value));
                return;
            }
            else if (type == typeof(double))
            {
                property.SetValue(obj, double.Parse(value));
                return;
            }
            else if (type == typeof(DateTime))
            {
                property.SetValue(obj, DateTime.Parse(value));
                return;
            }
            throw new InvalidOperationException("Bad type : " + type.Name);
        }
    }
}