﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Shawn.Utils
{
    /// <summary>
    /// AutoCompleteComboBox.xaml 的交互逻辑
    /// </summary>
    public partial class AutoCompleteComboBox : UserControl
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(AutoCompleteComboBox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, TextPropertyChangedCallback));

        private static void TextPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AutoCompleteComboBox c
                && e.NewValue is string v)
            {
                TextChanged(c, v);
            }
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set
            {
                if (value != Text)
                {
                    SetValue(TextProperty, value);
                    TextChanged(this, value);
                }
            }
        }

        static void TextChanged(AutoCompleteComboBox o, string newValue)
        {
            if ((o.Selections?.Count() ?? 0) == 0)
                return;
            if (string.IsNullOrWhiteSpace(newValue))
            {
                o.CbContent.IsDropDownOpen = false;
                o.Selections4Show = new ObservableCollection<string>(o.Selections);
            }
            else
            {

                o.Selections4Show = new ObservableCollection<string>(o.Selections
                    .Where(x => x.ToLower().IndexOf(newValue.ToLower()) >= 0 
                                || x.IsMatchPinyinKeywords(newValue, out var _)
                                ));
                if (o.Selections4Show?.Count() > 0)
                {
                    o.CbContent.IsDropDownOpen = true;
                    var tb = (TextBox)o.CbContent.Template.FindName("PART_EditableTextBox", o.CbContent);
                    tb?.Select(tb.Text.Length, 0);
                }
                else
                {
                    o.Selections4Show = new ObservableCollection<string>(o.Selections);
                    o.CbContent.IsDropDownOpen = false;
                }
            }
        }





        public static readonly DependencyProperty SelectionsProperty = DependencyProperty.Register(
            nameof(Selections), typeof(IEnumerable<string>), typeof(AutoCompleteComboBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SelectionsPropertyChangedCallback));

        private static void SelectionsPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AutoCompleteComboBox cb &&
                e.NewValue is IEnumerable<string> selections)
            {
                cb.Selections4Show = new ObservableCollection<string>(selections);
            }
            if (d is AutoCompleteComboBox cb2 &&
                e.NewValue == null)
            {
                cb2.Selections4Show = new ObservableCollection<string>();
            }
        }

        public IEnumerable<string> Selections
        {
            get => (IEnumerable<string>)GetValue(SelectionsProperty);
            set
            {
                SetValue(SelectionsProperty, value);
                if (Selections != null)
                    Selections4Show = new ObservableCollection<string>(Selections);
            }
        }


        public static readonly DependencyProperty Selections4ShowProperty = DependencyProperty.Register(
            nameof(Selections4Show), typeof(IEnumerable<string>), typeof(AutoCompleteComboBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public IEnumerable<string> Selections4Show
        {
            get => (IEnumerable<string>)GetValue(Selections4ShowProperty);
            set => SetValue(Selections4ShowProperty, value);
        }


        public AutoCompleteComboBox()
        {
            InitializeComponent();
            Grid.DataContext = this;
        }
    }
}
