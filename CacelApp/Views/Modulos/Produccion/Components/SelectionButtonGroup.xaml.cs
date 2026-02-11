using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using System.Text.Json;

namespace CacelApp.Views.Modulos.Produccion.Components
{
    public partial class SelectionButtonGroup : System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
        public static readonly RoutedEvent CheckedEvent = EventManager.RegisterRoutedEvent(
            "Checked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SelectionButtonGroup));

        public event RoutedEventHandler Checked
        {
            add { AddHandler(CheckedEvent, value); }
            remove { RemoveHandler(CheckedEvent, value); }
        }

        public SelectionButtonGroup()
        {
            InitializeComponent();
            LayoutRoot.DataContext = this;
        }

        #region Dependency Properties

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(SelectionButtonGroup),
                new PropertyMetadata(null));

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly DependencyProperty SelectedValueProperty =
            DependencyProperty.Register("SelectedValue", typeof(object), typeof(SelectionButtonGroup),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public object SelectedValue
        {
            get => GetValue(SelectedValueProperty);
            set => SetValue(SelectedValueProperty, value);
        }

        public static readonly DependencyProperty GroupNameProperty =
            DependencyProperty.Register("GroupName", typeof(string), typeof(SelectionButtonGroup), new PropertyMetadata("DefaultGroup"));

        public string GroupName
        {
            get => (string)GetValue(GroupNameProperty);
            set => SetValue(GroupNameProperty, value);
        }

        public static readonly DependencyProperty ThemeColorProperty =
            DependencyProperty.Register("ThemeColor", typeof(System.Windows.Media.Brush), typeof(SelectionButtonGroup), new PropertyMetadata(System.Windows.Media.Brushes.DodgerBlue));

        public System.Windows.Media.Brush ThemeColor
        {
            get => (System.Windows.Media.Brush)GetValue(ThemeColorProperty);
            set => SetValue(ThemeColorProperty, value);
        }

        public static readonly DependencyProperty DisplayModeProperty =
            DependencyProperty.Register("DisplayMode", typeof(string), typeof(SelectionButtonGroup), new PropertyMetadata("Single"));

        public string DisplayMode
        {
            get => (string)GetValue(DisplayModeProperty);
            set => SetValue(DisplayModeProperty, value);
        }

        public static readonly DependencyProperty RowsProperty =
            DependencyProperty.Register("Rows", typeof(int), typeof(SelectionButtonGroup), new PropertyMetadata(0));

        public int Rows
        {
            get => (int)GetValue(RowsProperty);
            set => SetValue(RowsProperty, value);
        }

        public static readonly DependencyProperty ColumnsProperty =
            DependencyProperty.Register("Columns", typeof(int), typeof(SelectionButtonGroup), new PropertyMetadata(0));

        public int Columns
        {
            get => (int)GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        public static readonly DependencyProperty CodeMemberPathProperty =
            DependencyProperty.Register("CodeMemberPath", typeof(string), typeof(SelectionButtonGroup), new PropertyMetadata("Ext.bie_codigo"));

        public string CodeMemberPath
        {
            get => (string)GetValue(CodeMemberPathProperty);
            set => SetValue(CodeMemberPathProperty, value);
        }

        public static readonly DependencyProperty ShowShortcutProperty =
            DependencyProperty.Register("ShowShortcut", typeof(bool), typeof(SelectionButtonGroup), new PropertyMetadata(false));

        public bool ShowShortcut
        {
            get => (bool)GetValue(ShowShortcutProperty);
            set => SetValue(ShowShortcutProperty, value);
        }

        #endregion

        private void UserControl_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Manejo de flechas (Izquierda/Derecha/Arriba/Abajo)
            if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Down)
            {
                var items = ItemsSource?.Cast<object>().ToList();
                if (items == null || items.Count == 0) return;

                int currentIndex = -1;
                // Buscar índice actual
                if (SelectedValue != null)
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        var val = GetPropertyValue(items[i], "Value");
                        if (val != null && val.ToString() == SelectedValue.ToString())
                        {
                            currentIndex = i;
                            break;
                        }
                    }
                }

                int newIndex = currentIndex;
                if (e.Key == Key.Right || e.Key == Key.Down)
                {
                    newIndex++;
                    if (newIndex >= items.Count) newIndex = 0; // Wrap around or clamp? Wrap is nicer for small lists
                }
                else if (e.Key == Key.Left || e.Key == Key.Up)
                {
                    newIndex--;
                    if (newIndex < 0) newIndex = items.Count - 1;
                }

                if (newIndex >= 0 && newIndex < items.Count)
                {
                    var item = items[newIndex];
                    var val = GetPropertyValue(item, "Value");
                    if (val != null)
                    {
                        SelectedValue = val;
                        RaiseEvent(new RoutedEventArgs(CheckedEvent, this));
                        e.Handled = true;
                        return;
                    }
                }
            }

            if (!ShowShortcut) return;

            int index = -1;
            if (e.Key >= Key.D1 && e.Key <= Key.D9)
                index = (int)e.Key - (int)Key.D1;
            else if (e.Key >= Key.NumPad1 && e.Key <= Key.NumPad9)
                index = (int)e.Key - (int)Key.NumPad1;

            if (index >= 0)
            {
                var items = ItemsSource?.Cast<object>().ToList();
                if (items != null && index < items.Count)
                {
                    var item = items[index];
                    var val = GetPropertyValue(item, "Value");
                    if (val != null)
                    {
                        SelectedValue = val;
                        RaiseEvent(new RoutedEventArgs(CheckedEvent, this));
                        e.Handled = true;
                    }
                }
            }
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.RadioButton rb && rb.Tag != null)
            {
                var val = GetPropertyValue(rb.Tag, "Value");
                if (val != null)
                {
                    SelectedValue = val;
                    RaiseEvent(new RoutedEventArgs(CheckedEvent, this));
                }
            }
        }

        private object GetPropertyValue(object src, string propName)
        {
            if (src == null) return null;
            if (src is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.Object && je.TryGetProperty(propName, out var element))
                {
                    return element.ValueKind switch
                    {
                        JsonValueKind.String => element.GetString(),
                        JsonValueKind.Number => element.GetDouble(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        _ => element.ToString()
                    };
                }
                return null;
            }
            var prop = src.GetType().GetProperty(propName);
            return prop?.GetValue(src, null);
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        #endregion
    }
}
