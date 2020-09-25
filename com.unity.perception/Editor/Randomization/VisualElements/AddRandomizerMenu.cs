using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine.Experimental.Perception.Randomization.Editor;
using UnityEngine.Experimental.Perception.Randomization.Randomizers;
using UnityEngine.UIElements;

namespace UnityEngine.Experimental.Perception.Randomization.VisualElements
{
    class AddRandomizerMenu : VisualElement
    {
        string m_CurrentPath = string.Empty;
        string currentPath
        {
            get => m_CurrentPath;
            set
            {
                m_CurrentPath = value;
                DrawDirectoryItems();
            }
        }

        string currentPathName
        {
            get
            {
                if (m_CurrentPath == string.Empty)
                    return "Randomizers";
                var pathItems = m_CurrentPath.Split('/');
                return pathItems[pathItems.Length - 1];
            }
        }

        string m_SearchString = string.Empty;
        string searchString
        {
            get => m_SearchString;
            set
            {
                m_SearchString = value;
                if (m_SearchString == string.Empty)
                    DrawDirectoryItems();
                else
                    DrawSearchItems();
            }
        }

        RandomizerList m_RandomizerList;
        VisualElement m_MenuElements;
        TextElement m_DirectoryName;
        Dictionary<string, List<MenuItem>> m_MenuItemsMap = new Dictionary<string, List<MenuItem>>();
        Dictionary<string, HashSet<string>> m_MenuDirectories = new Dictionary<string, HashSet<string>>();
        List<MenuItem> m_MenuItems = new List<MenuItem>();

        class MenuItem
        {
            public Type randomizerType;
            public string itemName;

            public MenuItem(Type randomizerType, string itemName)
            {
                this.randomizerType = randomizerType;
                this.itemName = itemName;
            }
        }

        sealed class MenuItemElement : TextElement
        {
            public MenuItemElement(MenuItem menuItem, AddRandomizerMenu menu)
            {
                text = menuItem.itemName;
                AddToClassList("menu-element");
                RegisterCallback<MouseUpEvent>(evt => menu.AddRandomizer(menuItem.randomizerType));
            }
        }

        sealed class MenuDirectoryElement : VisualElement
        {
            public MenuDirectoryElement(string directory, AddRandomizerMenu menu)
            {
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    $"{StaticData.uxmlDir}/Randomizer/MenuDirectoryElement.uxml").CloneTree(this);
                var pathItems = directory.Split('/');
                this.Q<TextElement>("directory").text = pathItems[pathItems.Length - 1];
                RegisterCallback<MouseUpEvent>(evt => menu.currentPath = directory);
            }
        }

        public AddRandomizerMenu(VisualElement parentElement, VisualElement button, RandomizerList randomizerList)
        {
            m_RandomizerList = randomizerList;
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/Randomizer/AddRandomizerMenu.uxml");
            template.CloneTree(this);
            style.position = new StyleEnum<Position>(Position.Absolute);

            var buttonPosition = button.worldBound.position;
            var top = math.min(buttonPosition.y, parentElement.worldBound.height - 300);
            style.top = top;
            style.left = buttonPosition.x;

            focusable = true;
            RegisterCallback<FocusOutEvent>(evt =>
            {
                if (evt.relatedTarget == null || ((VisualElement)evt.relatedTarget).FindCommonAncestor(this) != this)
                    ExitMenu();
            });

            m_DirectoryName = this.Q<TextElement>("directory-name");
            m_DirectoryName.RegisterCallback<MouseUpEvent>(evt => { AscendDirectory(); });

            var searchBar = this.Q<TextField>("search-bar");
            searchBar.schedule.Execute(() => searchBar.ElementAt(0).Focus());
            searchBar.RegisterValueChangedCallback(evt => searchString = evt.newValue);

            m_MenuElements = this.Q<VisualElement>("menu-options");

            CreateMenuItems();
            DrawDirectoryItems();
        }

        void ExitMenu()
        {
            parent.Remove(this);
        }

        void AddRandomizer(Type randomizerType)
        {
            m_RandomizerList.AddRandomizer(randomizerType);
            ExitMenu();
        }

        void AscendDirectory()
        {
            var pathItems = m_CurrentPath.Split('/');
            var path = pathItems[0];
            for (var i = 1; i < pathItems.Length - 1; i++)
                path = $"{path}/{pathItems[i]}";
            currentPath = path;
        }

        void DrawDirectoryItems()
        {
            m_DirectoryName.text = currentPathName;
            m_MenuElements.Clear();

            if (m_MenuDirectories.ContainsKey(currentPath))
            {
                var directories = m_MenuDirectories[currentPath];
                foreach (var directory in directories)
                    m_MenuElements.Add(new MenuDirectoryElement(directory, this));
            }

            if (m_MenuItemsMap.ContainsKey(currentPath))
            {
                var menuItems = m_MenuItemsMap[currentPath];
                foreach (var menuItem in menuItems)
                    m_MenuElements.Add(new MenuItemElement(menuItem, this));
            }
        }

        void DrawSearchItems()
        {
            m_DirectoryName.text = "Randomizers";
            m_MenuElements.Clear();

            var upperSearchString = searchString.ToUpper();
            foreach (var menuItem in m_MenuItems)
            {
                if (menuItem.itemName.ToUpper().Contains(upperSearchString))
                    m_MenuElements.Add(new MenuItemElement(menuItem, this));
            }
        }

        void CreateMenuItems()
        {
            var rootList = new List<MenuItem>();
            m_MenuItemsMap.Add(string.Empty, rootList);
            foreach (var randomizerType in StaticData.randomizerTypes)
            {
                if (m_RandomizerList.randomizerTypeSet.Contains(randomizerType))
                    continue;
                var menuAttribute = (AddRandomizerMenuAttribute)Attribute.GetCustomAttribute(randomizerType, typeof(AddRandomizerMenuAttribute));
                if (menuAttribute != null)
                {
                    var pathItems = menuAttribute.menuPath.Split('/');
                    if (pathItems.Length > 1)
                    {
                        var path = string.Empty;
                        var itemName = pathItems[pathItems.Length - 1];
                        for (var i = 0; i < pathItems.Length - 1; i++)
                        {
                            var childPath = $"{path}/{pathItems[i]}";
                            if (i < pathItems.Length - 1)
                            {
                                if (!m_MenuDirectories.ContainsKey(path))
                                    m_MenuDirectories.Add(path, new HashSet<string>());
                                m_MenuDirectories[path].Add(childPath);
                            }
                            path = childPath;
                        }

                        if (!m_MenuItemsMap.ContainsKey(path))
                            m_MenuItemsMap.Add(path, new List<MenuItem>());

                        var item = new MenuItem(randomizerType, itemName);
                        m_MenuItems.Add(item);
                        m_MenuItemsMap[path].Add(item);
                    }
                    else
                    {
                        if (pathItems.Length == 0)
                            throw new AssertionException("Empty randomizer menu path");
                        var item = new MenuItem(randomizerType, pathItems[0]);
                        m_MenuItems.Add(item);
                        rootList.Add(item);
                    }
                }
                else
                {
                    rootList.Add(new MenuItem(randomizerType, randomizerType.Name));
                }
            }
            m_MenuItems.Sort((item1, item2) => item1.itemName.CompareTo(item2.itemName));
        }
    }
}
