using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

public class ShaderSelectionDropdown : AdvancedDropdown
{
    Action<object> m_OnSelectedShaderPopup;
    Shader m_CurrentShader;

    public ShaderSelectionDropdown(Shader shader, Action<object> onSelectedShaderPopup)
        : base(new AdvancedDropdownState())
    {
        minimumSize = new Vector2(270, 308);
        m_CurrentShader = shader;
        m_OnSelectedShaderPopup = onSelectedShaderPopup;
    }

    protected override AdvancedDropdownItem BuildRoot()
    {
        var root = new AdvancedDropdownItem("Shaders");

        var shaders = ShaderUtil.GetAllShaderInfo();
        var shaderList = new List<string>();
        var legacyList = new List<string>();
        var notSupportedList = new List<string>();
        var failedCompilationList = new List<string>();

        foreach (var shader in shaders)
        {
            if (shader.name.StartsWith("Deprecated") || shader.name.StartsWith("Hidden"))
            {
                continue;
            }

            if (shader.hasErrors)
            {
                failedCompilationList.Add(shader.name);
                continue;
            }

            if (!shader.supported)
            {
                notSupportedList.Add(shader.name);
                continue;
            }

            if (shader.name.StartsWith("Legacy Shaders/"))
            {
                legacyList.Add(shader.name);
                continue;
            }

            shaderList.Add(shader.name);
        }

        shaderList.Sort((s1, s2) =>
        {
            var order = s2.Count(c => c == '/') - s1.Count(c => c == '/');
            if (order == 0)
            {
                order = s1.CompareTo(s2);
            }

            return order;
        });
        legacyList.Sort();
        notSupportedList.Sort();
        failedCompilationList.Sort();

        shaderList.ForEach(s => AddShaderToMenu("", root, s, s));
        if (legacyList.Any() || notSupportedList.Any() || failedCompilationList.Any())
            root.AddSeparator();
        legacyList.ForEach(s => AddShaderToMenu("", root, s, s));
        notSupportedList.ForEach(s => AddShaderToMenu("Not supported/", root, s, "Not supported/" + s));
        failedCompilationList.ForEach(s => AddShaderToMenu("Failed to compile/", root, s, "Failed to compile/" + s));

        return root;
    }

    protected override void ItemSelected(AdvancedDropdownItem item)
    {
        m_OnSelectedShaderPopup(((ShaderDropdownItem) item).fullName);
        
    }

    private void AddShaderToMenu(string prefix, AdvancedDropdownItem parent, string fullShaderName, string shaderName)
    {
        var shaderNameParts = shaderName.Split('/');
        if (shaderNameParts.Length > 1)
        {
            AddShaderToMenu(prefix, FindOrCreateChild(parent, shaderName), fullShaderName,
                shaderName.Substring(shaderNameParts[0].Length + 1));
        }
        else
        {
            var item = new ShaderDropdownItem(prefix, fullShaderName, shaderName);
            
            parent.AddChild(item);
            if (m_CurrentShader != null && m_CurrentShader.name == fullShaderName)
            {
                //m_DataSource.selectedIDs.Add(item.id);
            }
        }
    }

    private AdvancedDropdownItem FindOrCreateChild(AdvancedDropdownItem parent, string path)
    {
        var shaderNameParts = path.Split('/');
        var group = shaderNameParts[0];
        foreach (var child in parent.children)
        {
            if (child.name == group)
                return child;
        }

        var item = new AdvancedDropdownItem(group);
        parent.AddChild(item);
        return item;
    }

    private class ShaderDropdownItem : AdvancedDropdownItem
    {
        string m_FullName;
        string m_Prefix;
        public string fullName => m_FullName;
        public string prefix => m_Prefix;

        public ShaderDropdownItem(string prefix, string fullName, string shaderName)
            : base(shaderName)
        {
            m_FullName = fullName;
            m_Prefix = prefix;
            id = (prefix + fullName + shaderName).GetHashCode();
        }
    }

    // private class MaterialDropdownGUI : AdvancedDropdownGUI
    // {
    //     public MaterialDropdownGUI(AdvancedDropdownDataSource dataSource)
    //         : base(dataSource) {}
    //
    //     internal override void DrawItem(AdvancedDropdownItem item, string name, Texture2D icon, bool enabled, bool drawArrow, bool selected, bool hasSearch)
    //     {
    //         var newScriptItem = item as ShaderDropdownItem;
    //         if (hasSearch && newScriptItem != null)
    //         {
    //             name = string.Format("{0} ({1})", newScriptItem.name, newScriptItem.prefix + newScriptItem.fullName);
    //         }
    //         base.DrawItem(item, name, icon, enabled, drawArrow, selected, hasSearch);
    //     }
    // }
}