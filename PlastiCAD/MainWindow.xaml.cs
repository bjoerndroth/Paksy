    using PlastiCAD.Core;
    using PlastiCAD.Models;
    using System;
    using System.Collections.Generic;
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
using Microsoft.Win32;
using System.IO;
using System.Text.Json;
using System.Windows.Media.Media3D;


namespace PlastiCAD
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>


    public partial class MainWindow : Window
    {
        private readonly Stack<ProjectFile> undoStack = new Stack<ProjectFile>();

        private readonly Stack<ProjectFile> redoStack = new Stack<ProjectFile>();

        private Point lastMousePosition;

        private List<PlacedPart> copiedParts = new List<PlacedPart>();

        private bool isSelecting = false;

        private Point selectionStart;

        private Rectangle selectionRectangle;

        private Dictionary<PlacedPart, Vector3> dragStartPositions= new Dictionary<PlacedPart, Vector3>();

        private Point dragStartMousePosition;
        private Assembly assembly = new Assembly();
        private List<PlacedPart> selectedParts = new List<PlacedPart>();

        private PlacedPart SelectedPart =>
            selectedParts.Count == 1
                ? selectedParts[0]
                : null;

        private Part selectedPart;

        private const double Scale = 2.0;
        private const double SnapDistance = 12.0;

        private bool isDragging = false;
        private Vector3 dragOffset = new Vector3();
        private List<SnapResult> currentSnaps = new List<SnapResult>();
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

        private bool IsPositionOccupied( double x,    double y,    IEnumerable<PlacedPart> ignoredParts = null)
        {
            const double tolerance = 0.001;

            foreach (PlacedPart part in assembly.PlacedParts)
            {
                if (ignoredParts != null && ignoredParts.Contains(part))
                    continue;

                bool sameX =
                    Math.Abs(part.Transform.Position.X - x) < tolerance;

                bool sameY =
                    Math.Abs(part.Transform.Position.Y - y) < tolerance;

                if (sameX && sameY)
                    return true;
            }

            return false;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            RedrawScene();
        }
        private void PartsList_SelectionChanged(
         object sender,
         SelectionChangedEventArgs e)
        {
            if (PartsList.SelectedIndex < 0)
                return;

            selectedPart = PartLibrary.Parts[PartsList.SelectedIndex];

            StatusText.Text = "Ausgewählt: " + selectedPart.Name;
        }

        private void BuildArea_MouseMove(object sender, MouseEventArgs e)
        {
            Point p = e.GetPosition(BuildArea);
            lastMousePosition = p;

            if (isSelecting)
            {
                double left = Math.Min(selectionStart.X, p.X);
                double top = Math.Min(selectionStart.Y, p.Y);

                double width = Math.Abs(p.X - selectionStart.X);
                double height = Math.Abs(p.Y - selectionStart.Y);

                Canvas.SetLeft(selectionRectangle, left);
                Canvas.SetTop(selectionRectangle, top);

                selectionRectangle.Width = width;
                selectionRectangle.Height = height;

                return;
            }

            if (!isDragging || selectedParts.Count == 0)
                return;

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                isDragging = false;
                BuildArea.ReleaseMouseCapture();
                return;
            }

            double grid = Grider.CellSize * Scale;

            double deltaX = p.X - dragStartMousePosition.X;
            double deltaY = p.Y - dragStartMousePosition.Y;

            double snappedDeltaX =
                Math.Round(deltaX / grid) * grid;

            double snappedDeltaY =
                Math.Round(deltaY / grid) * grid;

            bool positionIsValid = true;

            // Erst prüfen, ob alle Zielzellen frei sind
            foreach (PlacedPart part in selectedParts)
            {
                if (!dragStartPositions.TryGetValue(part, out Vector3 start))
                    continue;

                double newX = start.X + snappedDeltaX;
                double newY = start.Y + snappedDeltaY;

                if (IsPositionOccupied(
                    newX,
                    newY,
                    selectedParts))
                {
                    positionIsValid = false;
                    break;
                }
            }

            // Nur bewegen, wenn die gesamte Auswahl Platz hat
            if (positionIsValid)
            {
                foreach (PlacedPart part in selectedParts)
                {
                    if (!dragStartPositions.TryGetValue(part, out Vector3 start))
                        continue;

                    part.Transform.Position.X =
                        start.X + snappedDeltaX;

                    part.Transform.Position.Y =
                        start.Y + snappedDeltaY;
                }

                if (selectedParts.Count == 1)
                {
                    RefreshSnaps(true);
                }
                else
                {
                    currentSnaps.Clear();
                }
            }
            else
            {
                StatusText.Text = "Zielposition ist belegt";
            }

            RedrawScene();
        }
        private int ConnectSelectedParts()
        {
            int connectionCount = 0;

            foreach (PlacedPart part in selectedParts)
            {
                currentSnaps = SnapEngine.FindSnaps(
                    assembly,
                    part,
                    Scale,
                    SnapDistance);

                connectionCount += ConnectCurrentSnaps();
            }

            return connectionCount;
        }

        private void BuildArea_MouseLeftButtonUp(
    object sender,
    MouseButtonEventArgs e)
        {
            if (isSelecting)
            {
                isSelecting = false;

                BuildArea.Children.Remove(selectionRectangle);

                selectedParts.Clear();

                Rect selection = new Rect(
                    Canvas.GetLeft(selectionRectangle),
                    Canvas.GetTop(selectionRectangle),
                    selectionRectangle.Width,
                    selectionRectangle.Height);

                foreach (PlacedPart part in assembly.PlacedParts)
                {
                    Rect partRect = new Rect(
                        part.Transform.Position.X,
                        part.Transform.Position.Y,
                        Grider.CellSize * Scale,
                        Grider.CellSize * Scale);

                    if (selection.Contains(partRect))
                        selectedParts.Add(part);
                }

                RedrawScene();

                return;
            }
            isDragging = false;
            BuildArea.ReleaseMouseCapture();

            int connectionCount = ConnectSelectedParts();

            dragStartPositions.Clear();

            StatusText.Text = connectionCount > 0
                ? $"{connectionCount} Verbindung(en)"
                : $"{selectedParts.Count} Bauteil(e) ausgewählt";

            RedrawScene();
        }
        private void BuildArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(BuildArea);

            double grid = Grider.CellSize * Scale;

            double targetX =
    Math.Floor(p.X / grid) * grid;

            double targetY =
                Math.Floor(p.Y / grid) * grid;


            lastMousePosition = p;
            // Prüfen, ob ein vorhandenes Teil angeklickt wurde
            PlacedPart clickedPart = GetPartAt(p);

            if (clickedPart == null && selectedPart == null)
            {
                isSelecting = true;

                selectionStart = p;

                selectionRectangle = new Rectangle
                {
                    Stroke = Brushes.DodgerBlue,
                    StrokeThickness = 1,
                    Fill = new SolidColorBrush(
                        Color.FromArgb(40, 30, 144, 255))
                };

                Canvas.SetLeft(selectionRectangle, p.X);
                Canvas.SetTop(selectionRectangle, p.Y);

                BuildArea.Children.Add(selectionRectangle);

                return;
            }

            if (clickedPart != null)
            {
                bool controlPressed =
                    (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

                if (controlPressed)
                {
                    // Strg+Klick: nur Auswahl ändern
                    if (selectedParts.Contains(clickedPart))
                        selectedParts.Remove(clickedPart);
                    else
                        selectedParts.Add(clickedPart);

                    StatusText.Text =
                        $"{selectedParts.Count} Bauteil(e) ausgewählt";

                    RedrawScene();
                    return;
                }

                // Wenn das angeklickte Teil nicht ausgewählt ist,
                // wird daraus wieder eine Einzelauswahl.
                if (!selectedParts.Contains(clickedPart))
                {
                    selectedParts.Clear();
                    selectedParts.Add(clickedPart);
                }

                SaveUndoState();

                foreach (PlacedPart part in selectedParts)
                {
                    DisconnectPart(part);
                }

                dragStartMousePosition = p;
                dragStartPositions.Clear();

                foreach (PlacedPart part in selectedParts)
                {
                    dragStartPositions[part] = new Vector3(
                        part.Transform.Position.X,
                        part.Transform.Position.Y,
                        part.Transform.Position.Z);
                }

                isDragging = true;
                BuildArea.CaptureMouse();


                StatusText.Text =
                    $"{selectedParts.Count} Bauteil(e) werden verschoben";

                RedrawScene();
                return;
            }
            // Wenn kein Teil getroffen wurde und kein Bibliotheksteil ausgewählt ist
            // Kein vorhandenes Teil getroffen.
            // Prüfen, ob ein Bibliotheksteil ausgewählt ist.
            if (selectedPart == null)
            {
                selectedParts.Clear();
                RedrawScene();
                return;
            }

            PlacedPart placed = new PlacedPart
            {
                Part = selectedPart
            };

        
            placed.Transform.Position = new Vector3(
                Math.Floor(p.X / grid) * grid,
                Math.Floor(p.Y / grid) * grid,
                0);

            placed.Sockets = selectedPart.CreateSockets();

            SaveUndoState();

            assembly.PlacedParts.Add(placed);



            selectedParts.Clear();
            selectedParts.Add(placed);

            RefreshSnaps(true);

            int connectionCount = ConnectCurrentSnaps();

            StatusText.Text = connectionCount > 0
                ? $"{connectionCount} Verbindung(en)"
                : "Bauteil gesetzt";

            Keyboard.Focus(BuildArea);

            RedrawScene();
        }
        private void RedrawScene()
        {
            BuildArea.Children.Clear();
            DrawGrid();

            foreach (PlacedPart placed in assembly.PlacedParts)
            {
                if (placed.Part is StructuralPart structuralPart)
                {
                    DrawStructuralPart(
                        placed,
                        structuralPart);
                }
            }

            RedrawWorld();
        }

        private void RedrawWorld()
        {
            while (WorldViewport.Children.Count > 1)
                WorldViewport.Children.RemoveAt(1);

            foreach (PlacedPart placed in assembly.PlacedParts)
            {
                if (!(placed.Part is StructuralPart part))
                    continue;

                double x = placed.Transform.Position.X / 100.0;
                double y = -placed.Transform.Position.Y / 100.0;
                double z = placed.Transform.Position.Z / 100.0;

                Point3D center = new Point3D(x, y, z);

                double radius = part.OuterDiameter / 200.0;
                double armLength = (part.Length / 2.0) / 100.0;

                // Kugel im Mittelpunkt für Winkel, T-Stück, Kreuz usw.
                if (part.DrawCenter)
                {
                    AddSphere(
                        center,
                        radius);
                }

                // Ein Rohrarm pro Socket
                foreach (Socket socket in placed.Sockets)
                {
                    Vector3 direction = socket.Direction;

                    // bestehende Paksy-Plan-Rotation mitnehmen
                    int zSteps =
                        (((placed.Rotation % 360) + 360) % 360) / 90;

                    for (int i = 0; i < zSteps; i++)
                        direction = direction.RotateZ90();

                    // zukünftige echte X/Y/Z-Rotation ebenfalls anwenden
                    direction =
                        placed.Transform.ApplyRotation(direction);

                    Point3D end = new Point3D(
                        center.X + direction.X * armLength,
                        center.Y - direction.Y * armLength,
                        center.Z + direction.Z * armLength);

                    AddCylinder(
                        center,
                        end,
                        radius);
                }
            }
        }

        private void AddSphere(
    Point3D center,
    double radius)
        {
            const int latitudeSegments = 12;
            const int longitudeSegments = 20;

            MeshGeometry3D mesh = new MeshGeometry3D();

            for (int lat = 0; lat <= latitudeSegments; lat++)
            {
                double theta =
                    Math.PI * lat / latitudeSegments;

                double sinTheta = Math.Sin(theta);
                double cosTheta = Math.Cos(theta);

                for (int lon = 0; lon <= longitudeSegments; lon++)
                {
                    double phi =
                        2 * Math.PI * lon / longitudeSegments;

                    double sinPhi = Math.Sin(phi);
                    double cosPhi = Math.Cos(phi);

                    double x =
                        center.X +
                        radius * sinTheta * cosPhi;

                    double y =
                        center.Y +
                        radius * cosTheta;

                    double z =
                        center.Z +
                        radius * sinTheta * sinPhi;

                    mesh.Positions.Add(
                        new Point3D(x, y, z));
                }
            }

            for (int lat = 0; lat < latitudeSegments; lat++)
            {
                for (int lon = 0; lon < longitudeSegments; lon++)
                {
                    int first =
                        lat * (longitudeSegments + 1) + lon;

                    int second =
                        first + longitudeSegments + 1;

                    mesh.TriangleIndices.Add(first);
                    mesh.TriangleIndices.Add(second);
                    mesh.TriangleIndices.Add(first + 1);

                    mesh.TriangleIndices.Add(second);
                    mesh.TriangleIndices.Add(second + 1);
                    mesh.TriangleIndices.Add(first + 1);
                }
            }

            DiffuseMaterial material =
                new DiffuseMaterial(Brushes.SteelBlue);

            GeometryModel3D model =
                new GeometryModel3D
                {
                    Geometry = mesh,
                    Material = material,
                    BackMaterial = material
                };

            WorldViewport.Children.Add(
                new ModelVisual3D
                {
                    Content = model
                });
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

        private void AddCylinder(
    Point3D start,
    Point3D end,
    double radius)
        {
            const int segments = 16;

            Vector3D axis = end - start;

            if (axis.Length == 0)
                return;

            axis.Normalize();

            Vector3D reference =
                Math.Abs(axis.Y) < 0.9
                ? new Vector3D(0, 1, 0)
                : new Vector3D(1, 0, 0);

            Vector3D side1 =
                Vector3D.CrossProduct(axis, reference);

            side1.Normalize();

            Vector3D side2 =
                Vector3D.CrossProduct(axis, side1);

            side2.Normalize();

            MeshGeometry3D mesh =
                new MeshGeometry3D();

            for (int i = 0; i < segments; i++)
            {
                double angle =
                    2 * Math.PI * i / segments;

                Vector3D offset =
                    side1 * (Math.Cos(angle) * radius) +
                    side2 * (Math.Sin(angle) * radius);

                mesh.Positions.Add(start + offset);
                mesh.Positions.Add(end + offset);
            }

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;

                int a = i * 2;
                int b = next * 2;
                int c = a + 1;
                int d = b + 1;

                mesh.TriangleIndices.Add(a);
                mesh.TriangleIndices.Add(b);
                mesh.TriangleIndices.Add(c);

                mesh.TriangleIndices.Add(c);
                mesh.TriangleIndices.Add(b);
                mesh.TriangleIndices.Add(d);
            }

            DiffuseMaterial material =
                new DiffuseMaterial(Brushes.SteelBlue);

            GeometryModel3D model =
                new GeometryModel3D
                {
                    Geometry = mesh,
                    Material = material,
                    BackMaterial = material
                };

            WorldViewport.Children.Add(
                new ModelVisual3D
                {
                    Content = model
                });
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



            Brush brush = selectedParts.Contains(placed)
              ? Brushes.Gold
              : Brushes.Blue;

            DrawArm(
                centerCell,
                FaceHelper.RotateFace(Face.Left, placed.Rotation),
                pipe.Length / 2,
                pipe.OuterDiameter,
                brush);

            DrawArm(
                centerCell,
                FaceHelper.RotateFace(Face.Right, placed.Rotation),
                pipe.Length / 2,
                pipe.OuterDiameter,
                brush);

            DrawSocket(
                 centerCell,
                 FaceHelper.RotateFace(Face.Left, placed.Rotation),
                 pipe.Length / 2,
                 placed.Sockets[0].IsConnected);

            DrawSocket(
                centerCell,
                FaceHelper.RotateFace(Face.Right, placed.Rotation),
                pipe.Length / 2,
                placed.Sockets[1].IsConnected);
        }


        private void DrawElbow(PlacedPart placed, Elbow elbow)
        {
            DrawGridCell(placed);

            Vector3 center = GetCellCenter(placed);

            Brush brush = selectedParts.Contains(placed)
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
                FaceHelper.RotateFace(Face.Left, placed.Rotation),
                elbow.LegLength,
                elbow.OuterDiameter,
                brush);

            DrawArm(
                center,
                FaceHelper.RotateFace(Face.Top, placed.Rotation),
                elbow.LegLength,
                elbow.OuterDiameter,
                brush);

            DrawSocket(
                center,
                FaceHelper.RotateFace(Face.Left, placed.Rotation),
                elbow.LegLength,
                placed.Sockets[0].IsConnected);

            DrawSocket(
                center,
                FaceHelper.RotateFace(Face.Top, placed.Rotation),
                elbow.LegLength,
                placed.Sockets[1].IsConnected);
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

            bool controlPressed = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

            if (controlPressed && e.Key == Key.Z)
            {
                Undo();

                e.Handled = true;
                return;
            }

            if (controlPressed && e.Key == Key.Y)
            {
                Redo();

                e.Handled = true;
                return;
            }


            if (controlPressed && e.Key == Key.S)
            {
                SaveProject();

                e.Handled = true;
                return;
            }

            if (controlPressed && e.Key == Key.O)
            {
                LoadProject();

                e.Handled = true;
                return;
            } 



            if (controlPressed && e.Key == Key.C)
            {
                CopySelection();
                e.Handled = true;
                return;
            }

            if (controlPressed && e.Key == Key.V)
            {
                PasteSelection();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape)
            {
                selectedPart = null;
                PartsList.SelectedIndex = -1;

                selectedParts.Clear();

                StatusText.Text = "Auswahlmodus";
                RedrawScene();

                e.Handled = true;
                return;
            }

            if (selectedParts.Count == 0)
                return;

            if (e.Key == Key.Delete)
            {
                DeleteSelection();

                e.Handled = true;
                return;
            }

            if (e.Key == Key.R)
            {
                int angle =
                    (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift
                        ? -90
                        : 90;

                RotateSelection(angle);

                e.Handled = true;
                return;
            }

          
          
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

        private void DrawSocket(
    Vector3 center,
    Face face,
    double length,
    bool connected)
        {
            Ellipse socket = new Ellipse();

            socket.Width = 8;
            socket.Height = 8;

            socket.Fill = connected
             ? Brushes.Gold
             : Brushes.Red;

            switch (face)
            {
                case Face.Left:
                    Canvas.SetLeft(socket, center.X - length * Scale - 4);
                    Canvas.SetTop(socket, center.Y - 4);
                    break;

                case Face.Right:
                    Canvas.SetLeft(socket, center.X + length * Scale - 4);
                    Canvas.SetTop(socket, center.Y - 4);
                    break;

                case Face.Top:
                    Canvas.SetLeft(socket, center.X - 4);
                    Canvas.SetTop(socket, center.Y - length * Scale - 4);
                    break;

                case Face.Bottom:
                    Canvas.SetLeft(socket, center.X - 4);
                    Canvas.SetTop(socket, center.Y + length * Scale - 4);
                    break;
            }

            BuildArea.Children.Add(socket);
        }

        private void RefreshSnaps(bool applySnap)
        {
            if (SelectedPart == null)
            {
                currentSnaps.Clear();
                return;
            }

            // Erste Suche: einen passenden Anker finden
            currentSnaps = SnapEngine.FindSnaps(
                assembly,
                SelectedPart,
                Scale,
                SnapDistance);

            if (applySnap && currentSnaps.Count > 0)
            {
                // Nur einmal positionieren
                SnapEngine.ApplySnap(
                    SelectedPart,
                    currentSnaps[0],
                    Scale);

                // Wichtig:
                // Nach dem Einrasten alle nun passenden Sockets neu suchen
                currentSnaps = SnapEngine.FindSnaps(
                    assembly,
                    SelectedPart,
                    Scale,
                    SnapDistance);
            }
        }

        private int ConnectCurrentSnaps()
        {
            int connectionCount = 0;

            foreach (SnapResult snap in currentSnaps)
            {
                if (snap.MovingSocket.IsConnected ||
                    snap.OtherSocket.IsConnected)
                {
                    continue;
                }

                assembly.Connections.Add(new Connection
                {
                    SocketA = snap.MovingSocket,
                    SocketB = snap.OtherSocket
                });

                snap.MovingSocket.IsConnected = true;
                snap.OtherSocket.IsConnected = true;

                snap.MovingSocket.ConnectedTo = snap.OtherSocket;
                snap.OtherSocket.ConnectedTo = snap.MovingSocket;

                connectionCount++;
            }

            currentSnaps.Clear();

            return connectionCount;
        }


        private void DrawStructuralPart(
      PlacedPart placed,
      StructuralPart part)
        {
            //DrawGridCell(placed);

            Vector3 center = GetCellCenter(placed);

            Brush brush = GetPartBrush(placed);

            if (part.DrawCenter)
            {
                DrawCenter(
                    center,
                    part.OuterDiameter,
                    brush);
            }

            foreach (Socket socket in placed.Sockets)
            {
                Face face =
                    FaceHelper.RotateFace(
                        socket.Face,
                        placed.Rotation);

                DrawArm(
                    center,
                    face,
                    part.Length / 2,
                    part.OuterDiameter,
                    brush);

                DrawSocket(
                    center,
                    face,
                    part.Length / 2,
                    socket.IsConnected);
            }
        }

        private Brush GetPartBrush(PlacedPart placed)
        {
            return selectedParts.Contains(placed)
       ? Brushes.LimeGreen
       : Brushes.Blue;
        }
        private void DrawCenter(
    Vector3 center,
    double diameter,
    Brush brush)
        {
            Ellipse circle = new Ellipse
            {
                Width = diameter * Scale,
                Height = diameter * Scale,
                Fill = brush
            };

            Canvas.SetLeft(
                circle,
                center.X - circle.Width / 2);

            Canvas.SetTop(
                circle,
                center.Y - circle.Height / 2);

            BuildArea.Children.Add(circle);
        }

        private bool NeedsCenterCircle(PlacedPart placed)
        {
            if (placed.Sockets.Count < 2)
                return false;

            bool hasLeft = false;
            bool hasRight = false;
            bool hasTop = false;
            bool hasBottom = false;

            foreach (Socket socket in placed.Sockets)
            {
                Face face = FaceHelper.RotateFace(
                    socket.Face,
                    placed.Rotation);

                if (face == Face.Left)
                    hasLeft = true;
                else if (face == Face.Right)
                    hasRight = true;
                else if (face == Face.Top)
                    hasTop = true;
                else if (face == Face.Bottom)
                    hasBottom = true;
            }

            bool horizontalLine = hasLeft && hasRight;
            bool verticalLine = hasTop && hasBottom;

            return !horizontalLine && !verticalLine;
        }

        private void DeleteSelectedPart()
        {
            PlacedPart part = SelectedPart;

            if (part == null)
                return;

            DisconnectPart(part);

            assembly.PlacedParts.Remove(part);

            selectedParts.Clear();
            currentSnaps.Clear();

            StatusText.Text = "Bauteil gelöscht";

            RedrawScene();
        }

        private void DeleteSelection()
        {
            if (selectedParts.Count == 0)
                return;

            SaveUndoState();

            foreach (PlacedPart part in selectedParts.ToList())
            {
                DisconnectPart(part);
                assembly.PlacedParts.Remove(part);
            }

            selectedParts.Clear();
            currentSnaps.Clear();

            StatusText.Text = "Bauteile gelöscht";

            RedrawScene();
        }
        private void CopySelection()
        {
            copiedParts.Clear();

            foreach (PlacedPart part in selectedParts)
            {
                PlacedPart copy = new PlacedPart
                {
                    Part = part.Part,
                    Rotation = part.Rotation
                };

                copy.Transform.Position = new Vector3(
                    part.Transform.Position.X,
                    part.Transform.Position.Y,
                    part.Transform.Position.Z);

                copy.Sockets = part.Part.CreateSockets();

                copiedParts.Add(copy);
            }

            StatusText.Text =
                $"{copiedParts.Count} Bauteil(e) kopiert";
        }

        private void PasteSelection()
        {
            if (copiedParts.Count == 0)
                return;

            double grid = Grider.CellSize * Scale;

            double minX = copiedParts.Min(
                part => part.Transform.Position.X);

            double minY = copiedParts.Min(
                part => part.Transform.Position.Y);

            double targetX =
                Math.Floor(lastMousePosition.X / grid) * grid;

            double targetY =
                Math.Floor(lastMousePosition.Y / grid) * grid;

            double offsetX = targetX - minX;
            double offsetY = targetY - minY;

            foreach (PlacedPart source in copiedParts)
            {
                double newX =
                    source.Transform.Position.X + offsetX;

                double newY =
                    source.Transform.Position.Y + offsetY;

                if (IsPositionOccupied(newX, newY))
                {
                    StatusText.Text =
                        "Einfügen nicht möglich: Rasterzelle ist belegt";

                    return;
                }
            }
            SaveUndoState();

            selectedParts.Clear();

            foreach (PlacedPart source in copiedParts)
            {
                PlacedPart pasted = new PlacedPart
                {
                    Part = source.Part,
                    Rotation = source.Rotation
                };

                pasted.Transform.Position = new Vector3(
                    source.Transform.Position.X + offsetX,
                    source.Transform.Position.Y + offsetY,
                    source.Transform.Position.Z);

                pasted.Sockets = source.Part.CreateSockets();

                assembly.PlacedParts.Add(pasted);
                selectedParts.Add(pasted);
            }

            int connectionCount = ConnectSelectedParts();

            StatusText.Text = connectionCount > 0
                ? $"{selectedParts.Count} Bauteil(e) eingefügt, {connectionCount} Verbindung(en)"
                : $"{selectedParts.Count} Bauteil(e) eingefügt";

            RedrawScene();
        }

        private void RotateSelection(int angle)
        {
            if (selectedParts.Count == 0)
                return;

            SaveUndoState();



            double grid = Grider.CellSize * Scale;

            double minX = selectedParts.Min(
                part => part.Transform.Position.X);

            double minY = selectedParts.Min(
                part => part.Transform.Position.Y);

            double maxX = selectedParts.Max(
                part => part.Transform.Position.X);

            double maxY = selectedParts.Max(
                part => part.Transform.Position.Y);

            // Mittelpunkt der Auswahl in Rasterkoordinaten
            double pivotX = (minX + maxX) / 2.0;
            double pivotY = (minY + maxY) / 2.0;

            foreach (PlacedPart part in selectedParts)
            {
                DisconnectPart(part);
            }

            foreach (PlacedPart part in selectedParts)
            {
                double relativeX =
                    part.Transform.Position.X - pivotX;

                double relativeY =
                    part.Transform.Position.Y - pivotY;

                double rotatedX;
                double rotatedY;

                if (angle == 90)
                {
                    rotatedX = -relativeY;
                    rotatedY = relativeX;
                }
                else
                {
                    // -90°
                    rotatedX = relativeY;
                    rotatedY = -relativeX;
                }
                part.Transform.Position.X =
                    Math.Round((pivotX + rotatedX) / grid,
                               MidpointRounding.AwayFromZero) * grid;

                part.Transform.Position.Y =
                    Math.Round((pivotY + rotatedY) / grid,
                               MidpointRounding.AwayFromZero) * grid;
                part.Rotation =
                    (part.Rotation + angle + 360) % 360;
            }

            int connectionCount = ConnectSelectedParts();

            StatusText.Text = connectionCount > 0
                ? $"{connectionCount} Verbindung(en)"
                : $"{selectedParts.Count} Bauteil(e) gedreht";

            RedrawScene();
        }

        private void SaveProject()
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Title = "PlastiCAD-Projekt speichern",
                Filter = "PlastiCAD-Projekt (*.plasticad)|*.plasticad|JSON-Datei (*.json)|*.json",
                DefaultExt = ".plasticad",
                AddExtension = true
            };

            if (dialog.ShowDialog() != true)
                return;

            ProjectFile project = new ProjectFile();

            foreach (PlacedPart placed in assembly.PlacedParts)
            {
                project.Parts.Add(new PlacedPartData
                {
                    PartName = placed.Part.Name,

                    X = placed.Transform.Position.X,
                    Y = placed.Transform.Position.Y,
                    Z = placed.Transform.Position.Z,

                    Rotation = placed.Rotation,

                    RotationX = placed.Transform.Rotation.X,
                    RotationY = placed.Transform.Rotation.Y,
                    RotationZ = placed.Transform.Rotation.Z
                });
            }

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(project, options);

            File.WriteAllText(dialog.FileName, json);

            StatusText.Text =
                $"{project.Parts.Count} Bauteil(e) gespeichert";
        }

        private void LoadProject()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "PlastiCAD-Projekt öffnen",
                Filter = "PlastiCAD-Projekt (*.plasticad)|*.plasticad|JSON-Datei (*.json)|*.json"
            };

            if (dialog.ShowDialog() != true)
                return;

            string json = File.ReadAllText(dialog.FileName);

            ProjectFile project =
                JsonSerializer.Deserialize<ProjectFile>(json);

            if (project == null)
            {
                StatusText.Text = "Projekt konnte nicht geladen werden";
                return;
            }

            assembly.PlacedParts.Clear();
            assembly.Connections.Clear();

            selectedParts.Clear();
            currentSnaps.Clear();

            foreach (PlacedPartData data in project.Parts)
            {
                Part part = PartLibrary.Parts.FirstOrDefault(
                    item => item.Name == data.PartName);

                if (part == null)
                    continue;

                PlacedPart placed = new PlacedPart
                {
                    Part = part,
                    Rotation = data.Rotation
                };

                placed.Transform.Position = new Vector3(
                    data.X,
                    data.Y,
                    data.Z);

                placed.Transform.Rotation.X = data.RotationX;
                placed.Transform.Rotation.Y = data.RotationY;
                placed.Transform.Rotation.Z = data.RotationZ;

                placed.Sockets = part.CreateSockets();

                assembly.PlacedParts.Add(placed);
            }

            RebuildConnections();

            StatusText.Text =
                $"{assembly.PlacedParts.Count} Bauteil(e) geladen";

            RedrawScene();
        }

        private void RebuildConnections()
        {
            assembly.Connections.Clear();

            foreach (PlacedPart part in assembly.PlacedParts)
            {
                foreach (Socket socket in part.Sockets)
                {
                    socket.IsConnected = false;
                    socket.ConnectedTo = null;
                }
            }

            foreach (PlacedPart part in assembly.PlacedParts)
            {
                currentSnaps = SnapEngine.FindSnaps(
                    assembly,
                    part,
                    Scale,
                    SnapDistance);

                ConnectCurrentSnaps();
            }

            currentSnaps.Clear();
        }

        private ProjectFile CreateProjectSnapshot()
        {
            ProjectFile project = new ProjectFile();

            foreach (PlacedPart placed in assembly.PlacedParts)
            {
                project.Parts.Add(new PlacedPartData
                {
                    PartName = placed.Part.Name,

                    X = placed.Transform.Position.X,
                    Y = placed.Transform.Position.Y,
                    Z = placed.Transform.Position.Z,

                    Rotation = placed.Rotation,

                     RotationX = placed.Transform.Rotation.X,
                    RotationY = placed.Transform.Rotation.Y,
                    RotationZ = placed.Transform.Rotation.Z
                });
            }

            return project;
        }

        private void RestoreProjectSnapshot(ProjectFile project)
        {
            assembly.PlacedParts.Clear();
            assembly.Connections.Clear();

            selectedParts.Clear();
            currentSnaps.Clear();

            foreach (PlacedPartData data in project.Parts)
            {
                Part part = PartLibrary.Parts.FirstOrDefault(
                    p => p.Name == data.PartName);

                if (part == null)
                    continue;

                PlacedPart placed = new PlacedPart
                {
                    Part = part,
                    Rotation = data.Rotation
                };

                placed.Transform.Position = new Vector3(
                    data.X,
                    data.Y,
                    data.Z);

                placed.Transform.Rotation.X = data.RotationX;
                placed.Transform.Rotation.Y = data.RotationY;
                placed.Transform.Rotation.Z = data.RotationZ;

                placed.Sockets = part.CreateSockets();

                assembly.PlacedParts.Add(placed);
            }

            RebuildConnections();

            RedrawScene();
        }

        private void SaveUndoState()
        {
            undoStack.Push(CreateProjectSnapshot());

            // Sobald etwas Neues geändert wird,
            // ist die alte Redo-Kette ungültig.
            redoStack.Clear();
        }

        private void Undo()
        {
            if (undoStack.Count == 0)
            {
                StatusText.Text = "Nichts rückgängig zu machen";
                return;
            }

            // Aktuellen Zustand für Redo merken
            redoStack.Push(CreateProjectSnapshot());

            ProjectFile previous =
                undoStack.Pop();

            RestoreProjectSnapshot(previous);

            StatusText.Text =
                $"Rückgängig ({undoStack.Count} weitere)";
        }

        private void Redo()
        {
            if (redoStack.Count == 0)
            {
                StatusText.Text = "Nichts wiederherzustellen";
                return;
            }

            // aktuellen Zustand wieder für Undo merken
            undoStack.Push(CreateProjectSnapshot());

            ProjectFile nextState = redoStack.Pop();

            RestoreProjectSnapshot(nextState);

            StatusText.Text = "Wiederhergestellt";
        }


    }
}

    

    
