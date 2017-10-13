using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;

namespace VisualSortWPF
{
    class NumericTextBox : TextBox
    {
        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);
            StringBuilder sb = new StringBuilder();
            int start = this.SelectionStart;
            int decimalCount = 0;
            bool inDecimal = false;
            for (int i = 0; i < this.Text.Length; i++)
            {
                if (this.Text[i] == '.' && !inDecimal && this.DecimalPlaces > 0)
                {
                    sb.Append(this.Text[i]);
                    inDecimal = true;
                }
                else if (char.IsNumber(this.Text, i))
                {
                    if (inDecimal)
                    {
                        if (decimalCount < this.DecimalPlaces)
                        {
                            sb.Append(this.Text[i]);
                            if (inDecimal) decimalCount++;
                        }
                    }
                    else
                    {
                        sb.Append(this.Text[i]);
                    }
                }
                else
                {
                    if (i < start) start--;
                }
            }
            this.Text = sb.ToString();
            this.SelectionStart = start;
        }

        private const decimal DefaultMinValue = 0,
            DefaultMaxValue = 100;
        private const int DefaultDecimalPlaces = 0;

        public decimal Minimum { get; set; }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(
                "Minimum", typeof(decimal), typeof(NumericTextBox),
                new FrameworkPropertyMetadata(DefaultMinValue)
            );

        public decimal Maximum { get; set; }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(
                "Maximum", typeof(decimal), typeof(NumericTextBox),
                new FrameworkPropertyMetadata(DefaultMaxValue)
            );

        public int DecimalPlaces { get; set; }

        public static readonly DependencyProperty DecimalPlacesProperty =
            DependencyProperty.Register(
                "DecimalPlaces", typeof(int), typeof(NumericTextBox),
                new FrameworkPropertyMetadata(DefaultDecimalPlaces)
            );
    }
}
