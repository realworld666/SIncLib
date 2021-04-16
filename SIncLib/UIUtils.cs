using System;
using UnityEngine;

namespace SIncLib
{
    public static class UIUtils
    {
        public static void NameColumn(GUIListView listView)
        {
            listView.AddColumn("Name", o =>
            {
                WorkItem item = o as WorkItem;
                Debug.Assert(item != null);
                return item.Name;
            }, (o, o1) =>
            {
                WorkItem item  = o as WorkItem;
                WorkItem item1 = o1 as WorkItem;
                Debug.Assert(item != null && item1 != null);
                return String.CompareOrdinal(item.Name, item1.Name);
            }, false);
        }
    }
}