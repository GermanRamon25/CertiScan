using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows;
using System.Windows.Controls;

namespace CertiScan
{
    public static class RichTextBoxHelper
    {
        public static readonly DependencyProperty DocumentProperty =
            DependencyProperty.RegisterAttached("Document", typeof(FlowDocument), typeof(RichTextBoxHelper),
            new FrameworkPropertyMetadata(null, OnDocumentChanged));

        public static FlowDocument GetDocument(DependencyObject obj)
        {
            return (FlowDocument)obj.GetValue(DocumentProperty);
        }

        public static void SetDocument(DependencyObject obj, FlowDocument value)
        {
            obj.SetValue(DocumentProperty, value);
        }

        private static void OnDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Ahora el programa sabe qué es un 'RichTextBox'
            if (d is RichTextBox rtb)
            {
                rtb.Document = (FlowDocument)e.NewValue;
            }
        }
    }
}