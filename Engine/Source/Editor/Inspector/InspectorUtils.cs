using System;
using System.Numerics;
using System.Reflection;
using System.Collections;
using Engine.Editor;
using ImGuiNET;

namespace Editor;

public static class InspectorUtil
{
	// 绘制组件对象，返回是否有修改；newBoxed 是修改后的值（结构体整体写回）
	public static bool DrawComponentObject(string header, Type t, object? boxed)
	{
		var open = ImGui.CollapsingHeader(header, ImGuiTreeNodeFlags.DefaultOpen);
		bool changed = false;

		if (!open)
			return false;

		//TODO 默认绘制内置组件，查找注册列表，绘制注册的组件
		object? tmp = boxed;
		if (InspectorReflection.TryDraw(header, t, ref tmp, out changed))
		{
			boxed = tmp;
			return changed;
		}
		
		changed = DrawComponentBody(t, boxed);

		return changed;
	}

	public static bool DrawComponentBody(Type t, object? boxed)
	{
		bool changed = false;
		object? tmp = boxed;
		if (InspectorReflection.TryDraw(t.Name, t, ref tmp, out changed))
		{
			boxed = tmp;
			return changed;
		}
		foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public))
		{
			var val = f.GetValue(boxed);
			if (DrawField(f.Name, f.FieldType, ref val))
			{
				f.SetValue(boxed, val);
				changed = true;
			}
		}
		// 属性（可写可读）
		foreach (var p in t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
		{
			if (!p.CanRead || !p.CanWrite) continue;
			var val = p.GetValue(boxed);
			if (DrawField(p.Name, p.PropertyType, ref val))
			{
				p.SetValue(boxed, val);
				changed = true;
			}
		}
		return changed;
	}

	public static bool DrawField(string label, Type type, ref object? val)
	{
		bool changed;
		if (InspectorReflection.TryDraw(label, type, ref val, out changed)) return changed;
		changed = false;
        
		// Array
		if (type.IsArray)
		{
			var elemType = type.GetElementType()!;
			val ??= Array.CreateInstance(elemType, 0);
			var arr = (Array)val;
			int count = arr.Length;
			if (ImGui.CollapsingHeader($"{label} [{count}]", ImGuiTreeNodeFlags.DefaultOpen))
			{
				if (ImGui.Button($"Add##{label}"))
				{
					var newArr = Array.CreateInstance(elemType, count + 1);
					for (int i = 0; i < count; i++) newArr.SetValue(arr.GetValue(i), i);
					newArr.SetValue(elemType.IsValueType ? Activator.CreateInstance(elemType)! : Activator.CreateInstance(elemType)!, count);
					val = newArr;
					arr = newArr;
					changed = true;
					count = arr.Length;
				}
				for (int i = 0; i < count; i++)
				{
					ImGui.PushID(i);
					if (ImGui.Button($"Remove##{label}_{i}"))
					{
						var newArr = Array.CreateInstance(elemType, count - 1);
						int idx = 0;
						for (int j = 0; j < count; j++) if (j != i) { newArr.SetValue(arr.GetValue(j), idx); idx++; }
						val = newArr;
						arr = newArr;
						changed = true;
						ImGui.PopID();
						continue;
					}
					object? item = arr.GetValue(i);
					if (DrawField($"{label}[{i}]", elemType, ref item))
					{
						arr.SetValue(item, i);
						changed = true;
					}
					ImGui.PopID();
				}
			}
			return changed;
		}

		if (type == typeof(bool))
		{
			bool v = val is bool b && b;
			if (ImGui.Checkbox(label, ref v)) { val = v; changed = true; }
		}
		else if (type == typeof(int))
		{
			int v = val is int i ? i : 0;
			if (ImGui.InputInt(label, ref v)) { val = v; changed = true; }
		}
		else if (type == typeof(float))
		{
			float v = val is float f ? f : 0f;
			if (ImGui.InputFloat(label, ref v)) { val = v; changed = true; }
		}
		else if (type == typeof(string))
		{
			string v = val as string ?? string.Empty;
			if (ImGui.InputText(label, ref v, 1024)) { val = v; changed = true; }
		}
		else if (type.IsEnum)
		{
			// 展示为下拉
			var names = Enum.GetNames(type);
			int idx = val != null ? Array.IndexOf(names, val.ToString()) : 0;
			if (idx < 0) idx = 0;
			if (ImGui.Combo(label, ref idx, names, names.Length))
			{
				val = Enum.Parse(type, names[idx]);
				changed = true;
			}
		}
		else if (type == typeof(Vector2))
		{
			var v = val is Vector2 v2 ? v2 : default;
			if (ImGui.InputFloat2(label, ref v)) { val = v; changed = true; }
		}
		else if (type.FullName == "System.Numerics.Vector3") // 若你有 Vector3
		{
			// 反射兼容 Vector3
			var v = val ?? Activator.CreateInstance(type);
			float x = (float)type.GetProperty("X")!.GetValue(v)!;
			float y = (float)type.GetProperty("Y")!.GetValue(v)!;
			float z = (float)type.GetProperty("Z")!.GetValue(v)!;
			var temp = new System.Numerics.Vector3(x, y, z);
			if (ImGui.InputFloat3(label, ref temp))
			{
				type.GetProperty("X")!.SetValue(v, temp.X);
				type.GetProperty("Y")!.SetValue(v, temp.Y);
				type.GetProperty("Z")!.SetValue(v, temp.Z);
				val = v;
				changed = true;
			}
		}
		else if (type.FullName == "Foster.Framework.Color" || type.Name == "Color")
		{
			// 常见 Color 结构体（0..255 或 0..1），这里以 0..1 浮点展示
			var v = val ?? Activator.CreateInstance(type);
			float r = GetFloat(type, v, "R");
			float g = GetFloat(type, v, "G");
			float b = GetFloat(type, v, "B");
			float a = HasMember(type, "A") ? GetFloat(type, v, "A") : 1f;

			var col = new Vector4(r, g, b, a);
			if (ImGui.ColorEdit4(label, ref col))
			{
				SetFloat(type, v, "R", col.X);
				SetFloat(type, v, "G", col.Y);
				SetFloat(type, v, "B", col.Z);
				if (HasMember(type, "A")) SetFloat(type, v, "A", col.W);
				val = v;
				changed = true;
			}
		}
		else if (type.IsValueType && !type.IsPrimitive)
		{
			// 简单递归：结构体嵌套
			if (ImGui.TreeNode(label))
			{
				var boxed = val ?? Activator.CreateInstance(type)!;
				bool innerChanged = false;

				foreach (var f in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
				{
					var fv = f.GetValue(boxed);
					if (DrawField(f.Name, f.FieldType, ref fv))
					{
						f.SetValue(boxed, fv);
						innerChanged = true;
					}
				}
				foreach (var p in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
				{
					if (!p.CanRead || !p.CanWrite) continue;
                    var pv = p.GetValue(boxed);
					if (DrawField(p.Name, p.PropertyType, ref pv))
					{
						p.SetValue(boxed, pv);
						innerChanged = true;
					}
				}
				if (innerChanged) { val = boxed; changed = true; }
				ImGui.TreePop();
			}
		}
		else
		{
			ImGui.Text($"{label}: {val ?? "null"}");
		}

		return changed;
	}

	static bool HasMember(Type t, string name)
	{
		return t.GetField(name) != null || t.GetProperty(name) != null;
	}

	static float GetFloat(Type t, object obj, string name)
	{
		var f = t.GetField(name);
		if (f != null) return ConvertTo01(f.GetValue(obj));
		var p = t.GetProperty(name);
		if (p != null) return ConvertTo01(p.GetValue(obj));
		return 0f;
	}

	static void SetFloat(Type t, object obj, string name, float value01)
	{
		var f = t.GetField(name);
		if (f != null) { f.SetValue(obj, ConvertFrom01(f.FieldType, value01)); return; }
		var p = t.GetProperty(name);
		if (p != null) { p.SetValue(obj, ConvertFrom01(p.PropertyType, value01)); return; }
	}

	static float ConvertTo01(object? v)
	{
		if (v == null) return 0f;
		var type = v.GetType();
		if (type == typeof(float)) return (float)v;
		if (type == typeof(double)) return (float)(double)v;
		if (type == typeof(byte)) return (byte)v / 255f;
		if (type == typeof(int)) return Math.Clamp((int)v, 0, 255) / 255f;
		return 0f;
	}

	static object ConvertFrom01(Type t, float v01)
	{
		if (t == typeof(float)) return v01;
		if (t == typeof(double)) return (double)v01;
		if (t == typeof(byte)) return (byte)Math.Round(Math.Clamp(v01, 0f, 1f) * 255f);
		if (t == typeof(int)) return (int)Math.Round(Math.Clamp(v01, 0f, 1f) * 255f);
		return v01;
	}
}