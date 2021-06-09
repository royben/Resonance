using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

namespace Resonance.Examples.RPC.Common
{
    public class DrawingBoard : Control
    {
        private Point _startPoint;
        private bool _isMouseDown;

        public RemoteRect CurrentRect
        {
            get { return (RemoteRect)GetValue(CurrentRectProperty); }
            set { SetValue(CurrentRectProperty, value); }
        }
        public static readonly DependencyProperty CurrentRectProperty =
            DependencyProperty.Register("CurrentRect", typeof(RemoteRect), typeof(DrawingBoard), new PropertyMetadata(new RemoteRect()));

        public ObservableCollection<RemoteRect> Rectangles
        {
            get { return (ObservableCollection<RemoteRect>)GetValue(RectanglesProperty); }
            set { SetValue(RectanglesProperty, value); }
        }
        public static readonly DependencyProperty RectanglesProperty =
            DependencyProperty.Register("Rectangles", typeof(ObservableCollection<RemoteRect>), typeof(DrawingBoard), new PropertyMetadata(null));

        public ICommand StartRectangleCommand
        {
            get { return (ICommand)GetValue(StartRectangleCommandProperty); }
            set { SetValue(StartRectangleCommandProperty, value); }
        }
        public static readonly DependencyProperty StartRectangleCommandProperty =
            DependencyProperty.Register("StartRectangleCommand", typeof(ICommand), typeof(DrawingBoard), new PropertyMetadata(null));

        public ICommand SizeRectangleCommand
        {
            get { return (ICommand)GetValue(SizeRectangleCommandProperty); }
            set { SetValue(SizeRectangleCommandProperty, value); }
        }
        public static readonly DependencyProperty SizeRectangleCommandProperty =
            DependencyProperty.Register("SizeRectangleCommand", typeof(ICommand), typeof(DrawingBoard), new PropertyMetadata(null));

        public ICommand FinishRectangleCommand
        {
            get { return (ICommand)GetValue(FinishRectangleCommandProperty); }
            set { SetValue(FinishRectangleCommandProperty, value); }
        }
        public static readonly DependencyProperty FinishRectangleCommandProperty =
            DependencyProperty.Register("FinishRectangleCommand", typeof(ICommand), typeof(DrawingBoard), new PropertyMetadata(null));

        public DrawingBoard()
        {
            Rectangles = new ObservableCollection<RemoteRect>();
        }

        static DrawingBoard()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DrawingBoard), new FrameworkPropertyMetadata(typeof(DrawingBoard)));
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);

            _startPoint = e.GetPosition(this);
            CurrentRect = new RemoteRect()
            {
                X = _startPoint.X,
                Y = _startPoint.Y,
            };

            StartRectangleCommand?.Execute(new RemotePoint() { X = _startPoint.X, Y = _startPoint.Y });
            _isMouseDown = true;
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);

            if (_isMouseDown)
            {
                Point position = e.GetPosition(this);

                double l = Math.Min(_startPoint.X, position.X);
                double t = Math.Min(_startPoint.Y, position.Y);
                double r = Math.Max(_startPoint.X, position.X);
                double b = Math.Max(_startPoint.Y, position.Y);

                CurrentRect = new RemoteRect()
                {
                    X = l,
                    Y = t,
                    Width = r - l,
                    Height = b - t
                };

                SizeRectangleCommand?.Execute(CurrentRect);
            }
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);

            if (_isMouseDown)
            {
                _isMouseDown = false;
                Rectangles.Add(CurrentRect);

                FinishRectangleCommand?.Execute(CurrentRect);

                CurrentRect = new RemoteRect();
            }
        }
    }
}
