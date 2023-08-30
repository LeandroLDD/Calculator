using MathNet.Symbolics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Printing;
using System.Text;
using System.Text.RegularExpressions;
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
using MathNet.Symbolics;
using static MathNet.Symbolics.VisualExpression;
using Expr = MathNet.Symbolics.Expression;
using static Microsoft.FSharp.Core.ByRefKinds;
using System.Diagnostics;
using System.IO;

namespace Calculadora
{
    public partial class MainWindow : Window
    {
        private KeyPanel panel;
        private const double FontSizeOperationsScreen = 40;
        private const double FontSizeResultScreen = 25;
        public MainWindow()
        {
            InitializeComponent();

            /*
             TERMINE DE INTEGRAR TODO LO DE LAS RAICES, AHORA TENGO QUE MEJORAR LA EFICIENCIA Y LOGICA DE LOS METODOS.
            POR LO QUE SÉ, HAY QUE MEJORAR "getTermFromIndexTest" Y "replaceTermFromIndex"
             */
            panel = new KeyPanel();
            this.DataContext = panel;

            TextBoxScreen.Focus();
        }

        private void ButtonPanel_Click(object sender, RoutedEventArgs e)
        {
            Button buttonPanel;
            buttonPanel = (Button)sender;

            if (!char.TryParse(buttonPanel.Content.ToString(), out char key)) key = ' ';

            insertElement(key);
        }
        private void TextBoxScreen_KeyDown(object sender, KeyEventArgs e)
        {
            char key;
            key = KeyPanel.standarizeOperation(e.Key);
            e.Handled = true;

            if (e.Key == Key.Left || e.Key == Key.Right) e.Handled = false;

            else {
                if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && e.Key == Key.V) e.Handled = false;

                else insertElement(key);
            }
        }
        private void TextBoxScreen_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            e.CancelCommand();

            if (e.DataObject.GetData(typeof(string)) is string contentPastedStr)
            {
                insertElement(contentPastedStr);
            }
        }
        private bool insertElement(char key)
        {
            bool ok;
            int selectedIndex;

            TextBoxScreen.Focus();
            selectedIndex = TextBoxScreen.CaretIndex;

            if(ok = panel.Insert(key, selectedIndex, out int indexToSelected))
            {
                selectedIndex = indexToSelected;
                TextBoxScreen.CaretIndex = selectedIndex;
            }

            return ok;
        }
        private void insertElement(string content)
        {
            foreach (var valueKey in content)
            {
                insertElement(valueKey);
            }
        }
        private void TextBoxScreen_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!(sender is TextBox textControl) || !double.TryParse(textControl.Tag.ToString(), out double fontSizeOriginal))
            {
                return;
            }

            if (textControl.Text.Length > 16)
            {
                fontSizeOriginal -= 15;
            }
            else if (textControl.Text.Length > 14)
            {
                fontSizeOriginal -= 10;
            }
            else if (textControl.Text.Length > 12)
            {
                fontSizeOriginal -= 5;
            }

            textControl.FontSize = fontSizeOriginal;
        }


        private void Window_Initialized(object sender, EventArgs e)
        {
            TextBoxScreen.FontSize = FontSizeOperationsScreen;
            TextBlockResultScreen.FontSize = FontSizeResultScreen;

            TextBoxScreen.Tag = FontSizeOperationsScreen;
            TextBlockResultScreen.Tag = FontSizeResultScreen;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            /*ESTABA TRATANDO DE CENTRAR LA VENTANA CUANDO SE MAXIMIZA*/
        }
    }
}
