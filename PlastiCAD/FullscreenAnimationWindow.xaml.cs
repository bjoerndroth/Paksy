using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace PlastiCAD
{
    public partial class FullscreenAnimationWindow : Window
    {
        private readonly List<Visual3D> borrowedVisuals = new List<Visual3D>();
        private Viewport3D sourceViewport;
        private DispatcherTimer timer;

        private Point3D orbitTarget;
        private Vector3D orbitOffset;
        private Vector3D orbitUp = new Vector3D(0, 1, 0);
        private char orbitAxis = 'Y';
        private const double OrbitSpeedDegrees = 0.37;
        private bool isMouseOrbiting;
        private Point lastMouse;
        public FullscreenAnimationWindow()
        {
            InitializeComponent();
        }

        public void Start(
            Viewport3D source,
            Point3D target,
            double distance,
            double height)
        {
            sourceViewport = source;
            orbitTarget = target;
            orbitAxis = 'Y';
            orbitUp = new Vector3D(0, 1, 0);

            double startAngle = Math.PI * 0.25;
            double safeDistance = Math.Max(distance, 0.3);

            orbitOffset = new Vector3D(
                Math.Cos(startAngle) * safeDistance,
                height,
                Math.Sin(startAngle) * safeDistance);

            BorrowVisuals();
            ApplyCamera();

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            timer.Tick += Timer_Tick;
            timer.Start();
            ShowHelpBriefly();
        }
        private DispatcherTimer helpTimer;

        private void ShowHelpBriefly()
        {
            HelpText.Opacity = 1;
            HelpText.Visibility = Visibility.Visible;

            helpTimer?.Stop();
            helpTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(4)
            };
            helpTimer.Tick += (s, e) =>
            {
                helpTimer.Stop();

                var fade = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(0.8)
                };
                fade.Completed += (s2, e2) =>
                {
                    HelpText.Visibility = Visibility.Collapsed;
                };

                HelpText.BeginAnimation(OpacityProperty, fade);
            };
            helpTimer.Start();
        }
        private void BorrowVisuals()
        {
            if (sourceViewport == null)
                return;

            while (sourceViewport.Children.Count > 0)
            {
                Visual3D visual = sourceViewport.Children[0];
                sourceViewport.Children.RemoveAt(0);
                borrowedVisuals.Add(visual);
                AnimationViewport.Children.Add(visual);
            }
        }

        private void ReturnVisuals()
        {
            if (sourceViewport == null)
                return;

            foreach (Visual3D visual in borrowedVisuals)
            {
                AnimationViewport.Children.Remove(visual);
                sourceViewport.Children.Add(visual);
            }

            borrowedVisuals.Clear();
        }

        private void ApplyCamera()
        {
            Point3D position = orbitTarget + orbitOffset;
            AnimationCamera.Position = position;
            AnimationCamera.LookDirection = orbitTarget - position;
            AnimationCamera.UpDirection = orbitUp;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            Vector3D axis = orbitAxis switch
            {
                'X' => new Vector3D(1, 0, 0),
                'Z' => new Vector3D(0, 0, 1),
                _ => new Vector3D(0, 1, 0)
            };

            RotateTransform3D rotation = new RotateTransform3D(
                new AxisAngleRotation3D(axis, OrbitSpeedDegrees));

            orbitOffset = rotation.Transform(orbitOffset);
            orbitUp = rotation.Transform(orbitUp);

            if (orbitUp.Length > 0)
                orbitUp.Normalize();

            ApplyCamera();
        }

        private void Stop()
        {
            if (timer != null)
            {
                timer.Stop();
                timer.Tick -= Timer_Tick;
                timer = null;
            }

            ReturnVisuals();
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape || e.Key == Key.F11)
            {
                Stop();
                return;
            }

            if (e.Key == Key.X)
                orbitAxis = 'X';
            else if (e.Key == Key.Y)
                orbitAxis = 'Y';
            else if (e.Key == Key.Z)
                orbitAxis = 'Z';

            if (e.Key == Key.H)
                ShowHelpBriefly();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isMouseOrbiting = true;
            lastMouse = e.GetPosition(this);
            CaptureMouse();
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isMouseOrbiting = false;
            ReleaseMouseCapture();
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isMouseOrbiting)
                return;

            Point now = e.GetPosition(this);
            double dx = now.X - lastMouse.X;
            double dy = now.Y - lastMouse.Y;
            lastMouse = now;

            double w = Math.Max(ActualWidth, 1);
            double h = Math.Max(ActualHeight, 1);

            double degA = 500.0 / w * dx;
            double degB = 500.0 / h * dy;

            char axisA;
            char axisB;

            switch (orbitAxis)
            {
                case 'X':
                    axisA = 'Y';
                    axisB = 'Z';
                    break;
                case 'Y':
                    axisA = 'X';
                    axisB = 'Z';
                    break;
                default:
                    axisA = 'X';
                    axisB = 'Y';
                    break;
            }

            ApplyOrbitRotation(axisA, degA);
            ApplyOrbitRotation(axisB, degB);
            ApplyCamera();
        }

        private void ApplyOrbitRotation(char axis, double degrees)
        {
            if (Math.Abs(degrees) < 0.0001)
                return;

            Vector3D dir = axis switch
            {
                'X' => new Vector3D(1, 0, 0),
                'Z' => new Vector3D(0, 0, 1),
                _ => new Vector3D(0, 1, 0)
            };

            RotateTransform3D rotation = new RotateTransform3D(
                new AxisAngleRotation3D(dir, degrees));

            orbitOffset = rotation.Transform(orbitOffset);
            orbitUp = rotation.Transform(orbitUp);
            if (orbitUp.Length > 0)
                orbitUp.Normalize();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (timer != null)
            {
                timer.Stop();
                timer.Tick -= Timer_Tick;
                timer = null;
            }

            ReturnVisuals();
            base.OnClosed(e);
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double factor = e.Delta > 0 ? 0.9 : 1.1;

            Vector3D newOffset = orbitOffset * factor;
            double length = newOffset.Length;

            // Nicht zu nah und nicht zu weit
            if (length < 0.25 || length > 80)
                return;

            orbitOffset = newOffset;
            ApplyCamera();
        }

    }
}