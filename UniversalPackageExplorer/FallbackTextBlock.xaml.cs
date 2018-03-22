using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UniversalPackageExplorer
{
    /// <summary>
    /// Interaction logic for FallbackTextBlock.xaml
    /// </summary>
    public partial class FallbackTextBlock : UserControl
    {
        public FallbackTextBlock()
        {
            InitializeComponent();
        }

        public static readonly DependencyPropertyKey ComputedTextPropertyKey = DependencyProperty.RegisterReadOnly(nameof(ComputedText), typeof(string), typeof(FallbackTextBlock), new PropertyMetadata());
        public static readonly DependencyProperty ComputedTextProperty = ComputedTextPropertyKey.DependencyProperty;
        public string ComputedText
        {
            get => (string)this.GetValue(ComputedTextProperty);
            private set => this.SetValue(ComputedTextPropertyKey, value);
        }
        public static readonly DependencyPropertyKey ComputedFontStylePropertyKey = DependencyProperty.RegisterReadOnly(nameof(ComputedFontStyle), typeof(FontStyle), typeof(FallbackTextBlock), new PropertyMetadata());
        public static readonly DependencyProperty ComputedFontStyleProperty = ComputedFontStylePropertyKey.DependencyProperty;
        public FontStyle ComputedFontStyle
        {
            get => (FontStyle)this.GetValue(ComputedFontStyleProperty);
            private set => this.SetValue(ComputedFontStylePropertyKey, value);
        }
        public static readonly DependencyPropertyKey ComputedForegroundPropertyKey = DependencyProperty.RegisterReadOnly(nameof(ComputedForeground), typeof(Brush), typeof(FallbackTextBlock), new PropertyMetadata());
        public static readonly DependencyProperty ComputedForegroundProperty = ComputedForegroundPropertyKey.DependencyProperty;
        public Brush ComputedForeground
        {
            get => (Brush)this.GetValue(ComputedForegroundProperty);
            private set => this.SetValue(ComputedForegroundPropertyKey, value);
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == TextProperty)
            {
                if (e.NewValue == null)
                {
                    this.ComputedText = this.FallbackText;
                    this.ComputedFontStyle = this.FallbackFontStyle;
                    this.ComputedForeground = this.FallbackForeground;
                }
                else
                {
                    this.ComputedText = this.Text;
                    this.ComputedFontStyle = this.FontStyle;
                    this.ComputedForeground = this.Foreground;
                }
            }
            else if (e.Property == FallbackTextProperty)
            {
                this.ComputedText = this.Text ?? this.FallbackText;
            }
            else if (e.Property == FontStyleProperty || e.Property == FallbackFontStyleProperty)
            {
                this.ComputedFontStyle = this.Text == null ? this.FallbackFontStyle : this.FontStyle;
            }
            else if (e.Property == ForegroundProperty || e.Property == FallbackForegroundProperty)
            {
                this.ComputedForeground = this.Text == null ? this.FallbackForeground : this.Foreground;
            }
        }

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(FallbackTextBlock));
        [Bindable(true)]
        public string Text
        {
            get => (string)this.GetValue(TextProperty);
            set => this.SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty FallbackTextProperty = DependencyProperty.Register(nameof(FallbackText), typeof(string), typeof(FallbackTextBlock));
        [Bindable(true)]
        public string FallbackText
        {
            get => (string)this.GetValue(FallbackTextProperty);
            set => this.SetValue(FallbackTextProperty, value);
        }

        public static readonly DependencyProperty FallbackFontStyleProperty = DependencyProperty.Register(nameof(FallbackFontStyle), typeof(FontStyle), typeof(FallbackTextBlock), new PropertyMetadata(FontStyles.Italic));
        [Bindable(true)]
        [Category("Appearance")]
        public FontStyle FallbackFontStyle
        {
            get => (FontStyle)this.GetValue(FallbackFontStyleProperty);
            set => this.SetValue(FallbackFontStyleProperty, value);
        }

        public static readonly DependencyProperty FallbackForegroundProperty = DependencyProperty.Register(nameof(FallbackForeground), typeof(Brush), typeof(FallbackTextBlock));
        [Bindable(true)]
        [Category("Appearance")]
        public Brush FallbackForeground
        {
            get => (Brush)this.GetValue(FallbackForegroundProperty);
            set => this.SetValue(FallbackForegroundProperty, value);
        }
    }
}
