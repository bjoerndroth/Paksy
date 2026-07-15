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


            if (isSelecting)
            {
                Point ppp = e.GetPosition(BuildArea);

                double left = Math.Min(selectionStart.X, ppp.X);
                double top = Math.Min(selectionStart.Y, ppp.Y);

                double width = Math.Abs(ppp.X - selectionStart.X);
                double height = Math.Abs(ppp.Y - selectionStart.Y);

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

            Point p = e.GetPosition(BuildArea);

            double grid = Grider.CellSize * Scale;

            double deltaX = p.X - dragStartMousePosition.X;
            double deltaY = p.Y - dragStartMousePosition.Y;

            double snappedDeltaX =
                Math.Round(deltaX / grid) * grid;

            double snappedDeltaY =
                Math.Round(deltaY / grid) * grid;

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

                dragStartMousePosition = p;
                dragStartPositions.Clear();

                foreach (PlacedPart part in selectedParts)
                {
                    dragStartPositions[part] = new Vector3(
                        part.Transform.Position.X,
                        part.Transform.Position.Y,
                        part.Transform.Position.Z);
                }

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

            double grid = Grider.CellSize * Scale;

            placed.Transform.Position = new Vector3(
                Math.Floor(p.X / grid) * grid,
                Math.Floor(p.Y / grid) * grid,
                0);

            placed.Sockets = selectedPart.CreateSockets();

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

            if (e.Key != Key.R)
                return;

            // Vor dem Drehen vorhandene Verbindungen dieses Teils lösen.
            DisconnectPart(SelectedPart);

            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                SelectedPart.Rotation =
                    (SelectedPart.Rotation + 270) % 360;
            }
            else
            {
                SelectedPart.Rotation =
                    (SelectedPart.Rotation + 90) % 360;
            }

            RefreshSnaps(true);

            int connectionCount = ConnectCurrentSnaps();

            StatusText.Text = connectionCount > 0
                ? $"{connectionCount} Verbindung(en)"
                : $"Drehung: {SelectedPart.Rotation}°";

            e.Handled = true;

            RedrawScene();
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

            double offset = Grider.CellSize * Scale;

            selectedParts.Clear();

            foreach (PlacedPart source in copiedParts)
            {
                PlacedPart pasted = new PlacedPart
                {
                    Part = source.Part,
                    Rotation = source.Rotation
                };

                pasted.Transform.Position = new Vector3(
                    source.Transform.Position.X + offset,
                    source.Transform.Position.Y + offset,
                    source.Transform.Position.Z);

                pasted.Sockets = source.Part.CreateSockets();

                assembly.PlacedParts.Add(pasted);
                selectedParts.Add(pasted);
            }

            StatusText.Text =
                $"{selectedParts.Count} Bauteil(e) eingefügt";

            RedrawScene();
        }
    }
}

    

    
