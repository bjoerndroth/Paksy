    using PlastiCAD.Models;
    using System;
    using System.Collections.Generic;
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
    using PlastiCAD.Core;


namespace PlastiCAD
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>


    public partial class MainWindow : Window
    {


        private Assembly assembly = new Assembly();
        private Part selectedPart;
        private PlacedPart selectedPlacedPart;
        private const double Scale = 2.0;
        private const double SnapDistance = 12.0;

        private bool isDragging = false;
        private Vector3 dragOffset = new Vector3();
        private SnapResult currentSnap;
        public MainWindow()

        {

            //test
            InitializeComponent();

            PartLibrary.Initialize();

            foreach (Part part in PartLibrary.Parts)
            {
                PartsList.Items.Add(part.Name);
            }

            Loaded += MainWindow_Loaded;
            KeyDown += MainWindow_KeyDown;
        }

        
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            RedrawScene();
        }
        private void PartsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedPart = PartLibrary.Parts[PartsList.SelectedIndex];

            StatusText.Text = "Ausgewählt: " + selectedPart.Name;
        }

        private void BuildArea_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging || selectedPlacedPart == null)
                return;

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                isDragging = false;
                BuildArea.ReleaseMouseCapture();
                return;
            }

            Point p = e.GetPosition(BuildArea);

            double grid = Grider.CellSize * Scale;
            selectedPlacedPart.Transform.Position.X =
                Math.Round((p.X - grid / 2) / grid) * grid;

            selectedPlacedPart.Transform.Position.Y =
                Math.Round((p.Y - grid / 2) / grid) * grid;

            currentSnap = SnapEngine.FindBestSnap(
    assembly,
    selectedPlacedPart,
    Scale,
    SnapDistance);

            if (currentSnap != null)
            {
                SnapEngine.ApplySnap(
                    selectedPlacedPart,
                    currentSnap,
                    Scale);
            }

            RedrawScene();
        }

        private void BuildArea_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            BuildArea.ReleaseMouseCapture();

            if (currentSnap != null &&
                !currentSnap.MovingSocket.IsConnected &&
                !currentSnap.OtherSocket.IsConnected)
            {
                assembly.Connections.Add(new Connection
                {
                    SocketA = currentSnap.MovingSocket,
                    SocketB = currentSnap.OtherSocket
                });

                currentSnap.MovingSocket.IsConnected = true;
                currentSnap.OtherSocket.IsConnected = true;

                currentSnap.MovingSocket.ConnectedTo = currentSnap.OtherSocket;
                currentSnap.OtherSocket.ConnectedTo = currentSnap.MovingSocket;

                StatusText.Text = "Verbunden";
            }
            else
            {
                StatusText.Text = "Bereit";
            }

            currentSnap = null;

            RedrawScene();
        }
        private void BuildArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(BuildArea);

            // Prüfen, ob ein vorhandenes Teil angeklickt wurde
            selectedPlacedPart = GetPartAt(p);



            if (selectedPlacedPart != null)
            {
                DisconnectPart(selectedPlacedPart);
                isDragging = true;

                BuildArea.CaptureMouse();
                double gridS = Grider.CellSize * Scale;
                selectedPlacedPart.Transform.Position.X =
                    Math.Round((p.X - gridS / 2) / gridS) * gridS;

                selectedPlacedPart.Transform.Position.Y =
                    Math.Round((p.Y - gridS / 2) / gridS) * gridS;

                dragOffset = new Vector3(
                    p.X - selectedPlacedPart.Transform.Position.X,
                    p.Y - selectedPlacedPart.Transform.Position.Y,
                    0);

                StatusText.Text = "Bauteil ausgewählt";

                RedrawScene();
                return;
            }


            // Wenn kein Teil getroffen wurde und kein Bibliotheksteil ausgewählt ist
            if (selectedPart == null)
                return;


            PlacedPart placed = new PlacedPart();

            placed.Part = selectedPart;

            double grid = Grider.CellSize * Scale;

            placed.Transform.Position = new Vector3(
                Math.Floor(p.X / grid) * grid,
                Math.Floor(p.Y / grid) * grid,
                0
            );

            placed.Sockets = selectedPart.CreateSockets();

            assembly.PlacedParts.Add(placed);
            //if (selectedPlacedPart == null)   return;

            selectedPlacedPart = placed;

            RedrawScene();
        }
        private void RedrawScene()
        {
            BuildArea.Children.Clear();
            DrawGrid();

            foreach (PlacedPart placed in assembly.PlacedParts)
            {
                if (placed.Part is Pipe pipe)
                {
                    DrawPipe(placed, pipe);
                }
                else if (placed.Part is Elbow elbow)
                {
                    DrawElbow(placed, elbow);
                }
            }
        }

        private PlacedPart GetPartAt(Point p)
        {
            double size = Grider.CellSize * Scale;

            foreach (PlacedPart placed in assembly.PlacedParts)
            {
                if (p.X >= placed.Transform.Position.X &&
                    p.X <= placed.Transform.Position.X + size &&
                    p.Y >= placed.Transform.Position.Y &&
                    p.Y <= placed.Transform.Position.Y + size)
                {
                    return placed;
                }
            }

            return null;
        }



        private void DisconnectPart(PlacedPart part)
        {
            foreach (Connection connection in assembly.Connections.ToList())
            {
                if (part.Sockets.Contains(connection.SocketA) ||
                    part.Sockets.Contains(connection.SocketB))
                {
                    connection.SocketA.IsConnected = false;
                    connection.SocketB.IsConnected = false;

                    connection.SocketA.ConnectedTo = null;
                    connection.SocketB.ConnectedTo = null;

                    assembly.Connections.Remove(connection);
                }
            }
        }
        private void DrawPipe(PlacedPart placed, Pipe pipe)
        {

            DrawGridCell(placed);

            // Mittelpunkt der Rasterzelle
            Vector3 centerCell = GetCellCenter(placed);



            // Jetzt erst das Rohr erzeugen
            Rectangle rect = new Rectangle();

            rect.Width = pipe.Length * Scale;
            rect.Height = pipe.OuterDiameter*Scale;

            rect.Fill = placed == selectedPlacedPart
                ? Brushes.Gold
                : Brushes.Blue;

            Canvas.SetLeft(rect,
                centerCell.X - rect.Width / 2);

            Canvas.SetTop(rect,
                centerCell.Y - rect.Height / 2);

            BuildArea.Children.Add(rect);

            // linker Socket
            // linker Socket


            Ellipse leftSocket = new Ellipse();
            leftSocket.Width = 8;
            leftSocket.Height = 8;

            if (placed.Sockets[0].IsConnected)
                leftSocket.Fill = Brushes.Green;
            else
                leftSocket.Fill = Brushes.Red;

            Canvas.SetLeft(leftSocket,
            centerCell.X - rect.Width / 2 - 4);

            Canvas.SetTop(leftSocket,
                centerCell.Y - 4);


            BuildArea.Children.Add(leftSocket);

            // rechter Socket
            Ellipse rightSocket = new Ellipse();
            rightSocket.Width = 8;
            rightSocket.Height = 8;
            if (placed.Sockets[1].IsConnected)
                rightSocket.Fill = Brushes.Green;
            else
                rightSocket.Fill = Brushes.Red;
            Canvas.SetLeft(rightSocket,
       centerCell.X + rect.Width / 2 - 4);

            Canvas.SetTop(rightSocket,
                centerCell.Y - 4);

            BuildArea.Children.Add(rightSocket);
        }


        private void DrawElbow(PlacedPart placed, Elbow elbow)
        {
            DrawGridCell(placed);

            Vector3 center = GetCellCenter(placed);

            Brush brush = placed == selectedPlacedPart
                ? Brushes.Gold
                : Brushes.Blue;

            // Mittelpunkt
            Ellipse circle = new Ellipse();

            circle.Width = elbow.OuterDiameter * Scale;
            circle.Height = elbow.OuterDiameter * Scale;
            circle.Fill = brush;

            Canvas.SetLeft(circle,
                center.X - circle.Width / 2);

            Canvas.SetTop(circle,
                center.Y - circle.Height / 2);

            BuildArea.Children.Add(circle);

            // Arme
            DrawArm(
                center,
                RotateFace(Face.Left, placed.Rotation),
                elbow.LegLength,
                elbow.OuterDiameter,
                brush);

            DrawArm(
                center,
                RotateFace(Face.Top, placed.Rotation),
                elbow.LegLength,
                elbow.OuterDiameter,
                brush);

            // Linker Socket
            Ellipse leftSocket = new Ellipse();
            leftSocket.Width = 8;
            leftSocket.Height = 8;

            leftSocket.Fill = placed.Sockets[0].IsConnected
                ? Brushes.Green
                : Brushes.Red;

            Canvas.SetLeft(leftSocket,
                center.X - elbow.LegLength * Scale - 4);

            Canvas.SetTop(leftSocket,
                center.Y - 4);

            BuildArea.Children.Add(leftSocket);

            // Oberer Socket
            Ellipse topSocket = new Ellipse();
            topSocket.Width = 8;
            topSocket.Height = 8;

            topSocket.Fill = placed.Sockets[1].IsConnected
                ? Brushes.Green
                : Brushes.Red;

            Canvas.SetLeft(topSocket,
                center.X - 4);

            Canvas.SetTop(topSocket,
                center.Y - elbow.LegLength * Scale - 4);

            BuildArea.Children.Add(topSocket);
        }
        private void DrawGridCell(PlacedPart placed)
        {
            Rectangle cell = new Rectangle();

            cell.Width = Grider.CellSize * Scale;
            cell.Height = Grider.CellSize * Scale;

            cell.Stroke = Brushes.LightGray;
            cell.StrokeThickness = 1;
            cell.Fill = Brushes.Transparent;

            Canvas.SetLeft(cell, placed.Transform.Position.X);
            Canvas.SetTop(cell, placed.Transform.Position.Y);

            BuildArea.Children.Add(cell);
        }

        private Vector3 GetCellCenter(PlacedPart placed)
        {
            return new Vector3(
                placed.Transform.Position.X + Grider.CellSize * Scale / 2,
                placed.Transform.Position.Y + Grider.CellSize * Scale / 2,
                0);
        }

        private void DrawGrid()
        {
            double grid = Grider.CellSize * Scale;

            double cross = 3; // halbe Kreuzgröße

            for (double x = 0; x < BuildArea.ActualWidth; x += grid)
            {
                for (double y = 0; y < BuildArea.ActualHeight; y += grid)
                {
                    Line h = new Line();
                    h.X1 = x - cross;
                    h.Y1 = y;
                    h.X2 = x + cross;
                    h.Y2 = y;
                    h.Stroke = Brushes.LightGray;
                    h.StrokeThickness = 1;

                    Line v = new Line();
                    v.X1 = x;
                    v.Y1 = y - cross;
                    v.X2 = x;
                    v.Y2 = y + cross;
                    v.Stroke = Brushes.LightGray;
                    v.StrokeThickness = 1;

                    BuildArea.Children.Add(h);
                    BuildArea.Children.Add(v);
                }
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (selectedPlacedPart == null)
                return;

            if (e.Key != Key.R)
                return;

            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                // -90°
                selectedPlacedPart.Rotation =
                    (selectedPlacedPart.Rotation + 270) % 360;
            }
            else
            {
                // +90°
                selectedPlacedPart.Rotation =
                    (selectedPlacedPart.Rotation + 90) % 360;
            }
            e.Handled = true;
            RedrawScene();
        }

        private Face RotateFace(Face face, int rotation)
        {
            int steps = ((rotation % 360) + 360) % 360 / 90;

            for (int i = 0; i < steps; i++)
            {
                switch (face)
                {
                    case Face.Left:
                        face = Face.Top;
                        break;

                    case Face.Top:
                        face = Face.Right;
                        break;

                    case Face.Right:
                        face = Face.Bottom;
                        break;

                    case Face.Bottom:
                        face = Face.Left;
                        break;
                }
            }
            //StatusText.Text =RotateFace(Face.Left, selectedPlacedPart.Rotation).ToString();
            return face;
        }

        private void DrawArm(
    Vector3 center,
    Face face,
    double length,
    double diameter,
    Brush brush)
        {
            Rectangle arm = new Rectangle();

            arm.Fill = brush;

            switch (face)
            {
                case Face.Left:

                    arm.Width = length * Scale;
                    arm.Height = diameter * Scale;

                    Canvas.SetLeft(arm,
                        center.X - arm.Width);

                    Canvas.SetTop(arm,
                        center.Y - arm.Height / 2);
                    break;

                case Face.Right:

                    arm.Width = length * Scale;
                    arm.Height = diameter * Scale;

                    Canvas.SetLeft(arm,
                        center.X);

                    Canvas.SetTop(arm,
                        center.Y - arm.Height / 2);
                    break;

                case Face.Top:

                    arm.Width = diameter * Scale;
                    arm.Height = length * Scale;

                    Canvas.SetLeft(arm,
                        center.X - arm.Width / 2);

                    Canvas.SetTop(arm,
                        center.Y - arm.Height);
                    break;

                case Face.Bottom:

                    arm.Width = diameter * Scale;
                    arm.Height = length * Scale;

                    Canvas.SetLeft(arm,
                        center.X - arm.Width / 2);

                    Canvas.SetTop(arm,
                        center.Y);
                    break;
            }

            BuildArea.Children.Add(arm);
        }
    }
}

    

    
