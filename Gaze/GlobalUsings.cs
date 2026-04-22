// Resolve WPF vs WinForms ambiguities when both UseWPF and UseWindowsForms are enabled
global using Application = System.Windows.Application;
global using UserControl = System.Windows.Controls.UserControl;
global using Color = System.Windows.Media.Color;
global using KeyEventArgs = System.Windows.Input.KeyEventArgs;
global using MessageBox = System.Windows.MessageBox;
global using Point = System.Windows.Point;
global using Size = System.Windows.Size;
global using Button = System.Windows.Controls.Button;
global using ToggleButton = System.Windows.Controls.Primitives.ToggleButton;
global using Clipboard = System.Windows.Clipboard;
global using TextBox = System.Windows.Controls.TextBox;
global using Label = System.Windows.Controls.Label;
global using Panel = System.Windows.Controls.Panel;
global using HorizontalAlignment = System.Windows.HorizontalAlignment;
global using VerticalAlignment = System.Windows.VerticalAlignment;
global using Cursor = System.Windows.Input.Cursor;
global using Cursors = System.Windows.Input.Cursors;
global using MouseEventArgs = System.Windows.Input.MouseEventArgs;

