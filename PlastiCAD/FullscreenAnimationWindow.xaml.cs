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
        private double orbitDistance;
        private double orbitHeight;
        private double orbitAngle;

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
            orbitDistance = Math.Max(distance, 0.3);
            orbitHeight = height;
            orbitAngle = Math.PI * 0.25;

            BorrowVisuals();
            ApplyCamera();

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void BorrowVisuals()
        {
            if (sourceViewport == null)
                return;

            // Licht + Modelle aus der World-Ansicht übernehmen
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
            Point3D position = GetCameraPosition();
            AnimationCamera.Position = position;
            AnimationCamera.LookDirection = orbitTarget - position;
            AnimationCamera.UpDirection = new Vector3D(0, 1, 0);
        }

        private Point3D GetCameraPosition()
        {
            return new Point3D(
                orbitTarget.X + Math.Cos(orbitAngle) * orbitDistance,
                orbitTarget.Y + orbitHeight,
                orbitTarget.Z + Math.Sin(orbitAngle) * orbitDistance);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            orbitAngle += 0.0065;
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
                Stop();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Stop();
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
    }
}