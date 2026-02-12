using System;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;

var type = typeof(SkinnedEffect);
var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
Console.WriteLine("SkinnedEffect properties:");
foreach (var p in props.OrderBy(p => p.Name))
    Console.WriteLine($"  {p.Name}: {p.PropertyType.Name}");
