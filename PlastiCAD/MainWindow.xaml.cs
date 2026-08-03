using Microsoft.Win32;
    using PlastiCAD.Core;
    using PlastiCAD.Models;
    using System;
    using System.Collections.Generic;
using System.IO;
    using System.Linq;

using System.Runtime.CompilerServices;
    using System.Text;
using System.Text.Json;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
    using System.Windows.Navigation;
    using System.Windows.Shapes;


namespace PlastiCAD
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>


    public partial class MainWindow : Window
    {
        Brush currentYZBrush =
    new SolidColorBrush(
        Color.FromArgb(
            150,
            255,
            190,
            40));
        private bool showMoveGrid = false;
        private ModelVisual3D dragGridVisual;
        private double? dragGridPlaneY;
        private PlacedPart dragGridReferencePart;
        
        Brush lineBrush =
    new SolidColorBrush(
        Color.FromArgb(
            8,
            160,
            160,
            160));
        private static readonly Brush PaksyRed =
    new SolidColorBrush(
        Color.FromRgb(
            235,
            45,
            45));
        private Point3D? worldPartDragStartPoint = null;
        private PlacedPart worldMouseDownPart = null;
        private bool worldPartWasDragged = false;
        private bool isWorldPartDragging = false;

        private Point worldPartDragStartMouse;

        private Dictionary<PlacedPart, Vector3> worldPartDragStartPositions =
            new Dictionary<PlacedPart, Vector3>();
        private bool worldCameraInitialized = false;

        private readonly Dictionary<Model3D, PlacedPart> worldPartMap =
    new Dictionary<Model3D, PlacedPart>();
        private bool isWorldOrbiting = false;
        private Point worldLastMousePosition;
        private bool isWorldPanning = false;

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
        private const double SnapDistance = 6.0;
        private double currentPlanZ = 0.0;

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

        private bool IsPositionOccupied(
    double x,
    double y,
    double z,
    Part movingPart,
    IEnumerable<PlacedPart> ignoredParts = null)
        {
            const double tolerance = 0.001;

            foreach (PlacedPart placed in assembly.PlacedParts)
            {
                if (ignoredParts != null &&
                    ignoredParts.Contains(placed))
                {
                    continue;
                }

                bool sameX =
                    Math.Abs(
                        placed.Transform.Position.X - x)
                    < tolerance;

                bool sameY =
                    Math.Abs(
                        placed.Transform.Position.Y - y)
                    < tolerance;

                bool sameZ =
                    Math.Abs(
                        placed.Transform.Position.Z - z)
                    < tolerance;

                if (!sameX || !sameY || !sameZ)
                    continue;

                bool movingIsOverlayPart =
                    movingPart is Wheel ||
                    movingPart is EndCap ||
                    movingPart is Plate;

                bool existingIsOverlayPart =
                    placed.Part is Wheel ||
                    placed.Part is EndCap ||
                    placed.Part is Plate;

                // Zusatzteil und Grundbauteil dürfen
                // dieselbe Rasterposition verwenden.
                if (movingIsOverlayPart != existingIsOverlayPart)
                    continue;

                // Zwei Grundbauteile dürfen dieselbe
                // Rasterposition nicht belegen.
                if (!movingIsOverlayPart)
                    return true;

                // Zusatzteile blockieren einander zunächst ebenfalls nicht.
                continue;
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
    start.Z,
    part.Part,
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
                    if (Math.Abs(part.Transform.Position.Z - currentPlanZ) >= 0.001)
                    {
                        continue;
                    }
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
                currentPlanZ);

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

            // Zuerst Flächenteile zeichnen,
            // damit Rohre und andere Bauteile darüberliegen.
            foreach (PlacedPart placed in assembly.PlacedParts)
            {
                if (placed.Part is WindowPlate windowPlate)
                {
                    DrawWindow2D(
                        placed,
                        windowPlate);

                    continue;
                }

                if (placed.Part is Plate plate)
                {
                    DrawPlate2D(
                        placed,
                        plate);
                }
            }

            // Danach die übrigen Bauteile zeichnen.
            foreach (PlacedPart placed in assembly.PlacedParts)
            {
                if (placed.Part is WindowPlate ||
                    placed.Part is Plate)
                {
                    continue;
                }

                if (placed.Part is Wheel wheel)
                {
                    DrawWheel(
                        placed,
                        wheel);

                    continue;
                }
                if (placed.Part is EndCap endCap)
                {
                    DrawEndCap2D(
                        placed,
                        endCap);

                    continue;
                }
                if (placed.Part is Cube cube)
                {
                    DrawCube2D(
                        placed,
                        cube);

                    continue;
                }
                if (placed.Part is BallConnector ball)
                {
                    DrawBallConnector2D(
                        placed,
                        ball);

                    continue;
                }
                if (placed.Part is StructuralPart structuralPart)
                {
                    DrawStructuralPart(
                        placed,
                        structuralPart);
                }
            }

            RedrawWorld();
        }

        private void DrawBallConnector2D(
    PlacedPart placed,
    BallConnector ball)
        {
            bool isCurrentLayer =
                Math.Abs(
                    placed.Transform.Position.Z - currentPlanZ)
                < 0.001;

            bool isSelected =
                selectedParts.Contains(placed);

            Vector3 center =
                GetCellCenter(placed);

            double diameter =
                ball.Diameter * Scale;

            double holeDiameter =
                ball.HoleDiameter * Scale;

            Brush ballBrush;

            if (isSelected)
            {
                ballBrush =
                    HighlightBrush(
                        GetWorldPartBrush(placed));
            }
            else if (isCurrentLayer)
            {
                ballBrush =
                    GetWorldPartBrush(placed);
            }
            else
            {
                ballBrush =
                    new SolidColorBrush(
                        Color.FromArgb(
                            45,
                            35,
                            140,
                            195));
            }

            Ellipse body =
                new Ellipse
                {
                    Width = diameter,
                    Height = diameter,
                    Fill = ballBrush,
                    Stroke = isSelected
                        ? Brushes.White
                        : Brushes.DarkBlue,
                    StrokeThickness = isSelected
                        ? 2.0
                        : 1.0
                };

            Canvas.SetLeft(
                body,
                center.X - diameter / 2.0);

            Canvas.SetTop(
                body,
                center.Y - diameter / 2.0);

            BuildArea.Children.Add(body);

            Brush holeBrush =
                isCurrentLayer
                    ? Brushes.Black
                    : new SolidColorBrush(
                        Color.FromArgb(
                            50,
                            0,
                            0,
                            0));

            Ellipse hole =
                new Ellipse
                {
                    Width = holeDiameter,
                    Height = holeDiameter,
                    Fill = holeBrush,
                    Stroke = Brushes.DarkSlateGray,
                    StrokeThickness = 1.0
                };

            Canvas.SetLeft(
                hole,
                center.X - holeDiameter / 2.0);

            Canvas.SetTop(
                hole,
                center.Y - holeDiameter / 2.0);

            BuildArea.Children.Add(hole);
        }
        private void DrawCube2D(
    PlacedPart placed,
    Cube cube)
        {
            bool isCurrentLayer =
    Math.Abs(
        placed.Transform.Position.Z - currentPlanZ)
    < 0.001;

            bool isSelected =
                selectedParts.Contains(placed);

            Brush cubeBrush;

            if (isSelected)
            {
                cubeBrush =
                    HighlightBrush(
                        GetWorldPartBrush(placed));
            }
            else if (isCurrentLayer)
            {
                cubeBrush =
                    GetWorldPartBrush(placed);
            }
            else
            {
                cubeBrush =
                    new SolidColorBrush(
                        Color.FromArgb(
                            45,
                            35,
                            140,
                            195));
            }

            Vector3 center =
                GetCellCenter(placed);

            double size =
                cube.Size * Scale;

            double holeDiameter =
                cube.HoleDiameter * Scale;

            

            Rectangle body =
                new Rectangle
                {
                    Width = size,
                    Height = size,
                    RadiusX =
                        cube.CornerRadius * Scale,
                    RadiusY =
                        cube.CornerRadius * Scale,
                    Fill = cubeBrush,
                    Stroke = Brushes.DarkBlue,
                    StrokeThickness = 1.0
                };

            Canvas.SetLeft(
                body,
                center.X - size / 2.0);

            Canvas.SetTop(
                body,
                center.Y - size / 2.0);

            BuildArea.Children.Add(body);

            Ellipse hole =
                new Ellipse
                {
                    Width = holeDiameter,
                    Height = holeDiameter,
                    Fill = Brushes.Black,
                    Stroke = Brushes.DarkSlateGray,
                    StrokeThickness = 1.0
                };

            Canvas.SetLeft(
                hole,
                center.X - holeDiameter / 2.0);

            Canvas.SetTop(
                hole,
                center.Y - holeDiameter / 2.0);

            BuildArea.Children.Add(hole);
        }
        private void DrawEndCap2D(
    PlacedPart placed,
    EndCap endCap)
        {
            bool isCurrentLayer =
                Math.Abs(
                    placed.Transform.Position.Z - currentPlanZ)
                < 0.001;

            if (!isCurrentLayer)
                return;

            Vector3 cellCenter =
                GetCellCenter(placed);

            Face capFace =
                FaceHelper.RotateFace(
                    Face.Right,
                    placed.Rotation);

            capFace =
                FaceHelper.RotateFace3D(
                    capFace,
                    placed.Transform.Rotation);

            bool isSelected =
                selectedParts.Contains(placed);

            Brush capBrush =
                isSelected
                    ? HighlightBrush(Brushes.Gold)
                    : Brushes.Gold;

            double capDiameter =
                endCap.OuterDiameter * Scale;

            double capLength =
                endCap.Length * Scale;

            double armEndDistance =
                Grider.CellSize * Scale / 2.0;

            Vector3 capCenter =
                new Vector3(
                    cellCenter.X,
                    cellCenter.Y,
                    cellCenter.Z);

            switch (capFace)
            {
                case Face.Right:
                    capCenter.X +=
                        armEndDistance + capLength / 2.0;

                    DrawEndCapSide2D(
                        capCenter,
                        capLength,
                        capDiameter,
                        true,
                        capBrush,
                        isSelected);

                    break;

                case Face.Left:
                    capCenter.X -=
                        armEndDistance + capLength / 2.0;

                    DrawEndCapSide2D(
                        capCenter,
                        capLength,
                        capDiameter,
                        true,
                        capBrush,
                        isSelected);

                    break;

                case Face.Top:
                    capCenter.Y -=
                        armEndDistance + capLength / 2.0;

                    DrawEndCapSide2D(
                        capCenter,
                        capLength,
                        capDiameter,
                        false,
                        capBrush,
                        isSelected);

                    break;

                case Face.Bottom:
                    capCenter.Y +=
                        armEndDistance + capLength / 2.0;

                    DrawEndCapSide2D(
                        capCenter,
                        capLength,
                        capDiameter,
                        false,
                        capBrush,
                        isSelected);

                    break;

                case Face.Front:
                case Face.Back:
                    DrawEndCapFront2D(
                        cellCenter,
                        capDiameter,
                        capBrush,
                        isSelected);

                    break;
            }
        }

        private void DrawEndCapSide2D(
    Vector3 center,
    double length,
    double diameter,
    bool horizontalAxis,
    Brush brush,
    bool isSelected)
        {
            Rectangle capShape =
                new Rectangle
                {
                    Fill = brush,
                    Stroke = isSelected
                        ? Brushes.White
                        : Brushes.Goldenrod,

                    StrokeThickness =
                        isSelected
                            ? 2.0
                            : 1.0,

                    RadiusX = 3.0,
                    RadiusY = 3.0
                };

            if (horizontalAxis)
            {
                capShape.Width =
                    length;

                capShape.Height =
                    diameter;
            }
            else
            {
                capShape.Width =
                    diameter;

                capShape.Height =
                    length;
            }

            Canvas.SetLeft(
                capShape,
                center.X - capShape.Width / 2.0);

            Canvas.SetTop(
                capShape,
                center.Y - capShape.Height / 2.0);

            BuildArea.Children.Add(
                capShape);
        }

        private void DrawEndCapFront2D(
    Vector3 center,
    double diameter,
    Brush brush,
    bool isSelected)
        {
            Ellipse capShape =
                new Ellipse
                {
                    Width = diameter,
                    Height = diameter,

                    Fill = brush,

                    Stroke = isSelected
                        ? Brushes.White
                        : Brushes.Goldenrod,

                    StrokeThickness =
                        isSelected
                            ? 2.0
                            : 1.0
                };

            Canvas.SetLeft(
                capShape,
                center.X - diameter / 2.0);

            Canvas.SetTop(
                capShape,
                center.Y - diameter / 2.0);

            BuildArea.Children.Add(
                capShape);
        }
        private void DrawPlate2D(
    PlacedPart placed,
    Plate plate)
        {
            bool isCurrentLayer =
                Math.Abs(
                    placed.Transform.Position.Z - currentPlanZ)
                < 0.001;

            if (!isCurrentLayer)
                return;

            double halfGrid =
                Grider.CellSize * Scale / 2.0;

            Vector3 cellCenter =
                GetCellCenter(placed);

            double plateWidth =
                plate.Width * Scale;

            double plateHeight =
                plate.Height * Scale;

            double plateThickness =
                Math.Max(
                    2.0,
                    plate.Thickness * Scale);

            Brush plateBrush =
                PaksyRed;

            if (selectedParts.Contains(placed))
            {
                plateBrush =
                    HighlightBrush(plateBrush);
            }

            Rectangle plateShape =
                new Rectangle
                {
                    Fill = plateBrush,
                    Stroke = selectedParts.Contains(placed)
                        ? Brushes.White
                        : Brushes.DarkRed,

                    StrokeThickness =
                        selectedParts.Contains(placed)
                            ? 2.0
                            : 1.0
                };

            double centerX;
            double centerY;

            switch (placed.PlateOrientation)
            {
                // Platte liegt waagerecht in der XY-Ebene.
                // In der Draufsicht sieht man die ganze Fläche.
                case 0:
                    centerX =
                        cellCenter.X + halfGrid;

                    centerY =
                        cellCenter.Y + halfGrid;

                    plateShape.Width =
                        plateWidth;

                    plateShape.Height =
                        plateHeight;

                    break;

                // Platte liegt in der XZ-Ebene.
                // In der Draufsicht erscheint sie als waagerechter Streifen.
                case 1:
                    centerX =
                        cellCenter.X + halfGrid;

                    centerY =
                        cellCenter.Y;

                    plateShape.Width =
                        plateWidth;

                    plateShape.Height =
                        plateThickness;

                    break;

                // Platte liegt in der YZ-Ebene.
                // In der Draufsicht erscheint sie als senkrechter Streifen.
                case 2:
                    centerX =
                        cellCenter.X;

                    centerY =
                        cellCenter.Y + halfGrid;

                    plateShape.Width =
                        plateThickness;

                    plateShape.Height =
                        plateHeight;

                    break;

                default:
                    return;
            }

            Canvas.SetLeft(
                plateShape,
                centerX - plateShape.Width / 2.0);

            Canvas.SetTop(
                plateShape,
                centerY - plateShape.Height / 2.0);

            BuildArea.Children.Add(
                plateShape);
        }

        private void DrawWindow2D(
    PlacedPart placed,
    WindowPlate windowPlate)
        {
            bool isCurrentLayer =
                Math.Abs(
                    placed.Transform.Position.Z - currentPlanZ)
                < 0.001;

            if (!isCurrentLayer)
                return;

            double halfGrid =
                Grider.CellSize * Scale / 2.0;

            Vector3 cellCenter =
                GetCellCenter(placed);

            double windowWidth =
                windowPlate.Width * Scale;

            double windowHeight =
                windowPlate.Height * Scale;

            double windowThickness =
                Math.Max(
                    2.0,
                    windowPlate.Thickness * Scale);

            bool isSelected =
                selectedParts.Contains(placed);

            Brush glassBrush =
                isSelected
                    ? new SolidColorBrush(
                        Color.FromArgb(
                            120,
                            210,
                            250,
                            255))
                    : new SolidColorBrush(
                        Color.FromArgb(
                            55,
                            170,
                            225,
                            255));

            Rectangle glassShape =
                new Rectangle
                {
                    Fill = glassBrush,

                    Stroke = isSelected
                        ? Brushes.White
                        : Brushes.SteelBlue,

                    StrokeThickness =
                        isSelected
                            ? 2.0
                            : 1.0
                };

            double centerX;
            double centerY;

            switch (placed.PlateOrientation)
            {
                // Fensterfläche von oben sichtbar
                case 0:
                    centerX =
                        cellCenter.X + halfGrid;

                    centerY =
                        cellCenter.Y + halfGrid;

                    glassShape.Width =
                        windowWidth;

                    glassShape.Height =
                        windowHeight;

                    break;

                // Fenster steht in XZ-Richtung
                case 1:
                    centerX =
                        cellCenter.X + halfGrid;

                    centerY =
                        cellCenter.Y;

                    glassShape.Width =
                        windowWidth;

                    glassShape.Height =
                        windowThickness;

                    break;

                // Fenster steht in YZ-Richtung
                case 2:
                    centerX =
                        cellCenter.X;

                    centerY =
                        cellCenter.Y + halfGrid;

                    glassShape.Width =
                        windowThickness;

                    glassShape.Height =
                        windowHeight;

                    break;

                default:
                    return;
            }

            Canvas.SetLeft(
                glassShape,
                centerX - glassShape.Width / 2.0);

            Canvas.SetTop(
                glassShape,
                centerY - glassShape.Height / 2.0);

            BuildArea.Children.Add(
                glassShape);

            // Mittelstrich nur zeichnen, wenn die Fensterfläche
            // in der Draufsicht vollständig sichtbar ist.
            if (placed.PlateOrientation == 0)
            {
                double barWidth =
                    Math.Max(
                        1.0,
                        windowPlate.CenterBarWidth * Scale);

                Rectangle centerBar =
                    new Rectangle
                    {
                        Width = barWidth,
                        Height = windowHeight,
                        Fill = new SolidColorBrush(
                            Color.FromArgb(
                                180,
                                120,
                                155,
                                170))
                    };

                Canvas.SetLeft(
                    centerBar,
                    centerX - centerBar.Width / 2.0);

                Canvas.SetTop(
                    centerBar,
                    centerY - centerBar.Height / 2.0);

                BuildArea.Children.Add(
                    centerBar);
            }
        }
        private void DrawWheel(
    PlacedPart placed,
    Wheel wheel)
        {
            bool isCurrentLayer =
                Math.Abs(
                    placed.Transform.Position.Z - currentPlanZ)
                < 0.001;

            if (!isCurrentLayer)
                return;

            Vector3 cellCenter = GetCellCenter(placed);

            Face wheelFace =
                FaceHelper.RotateFace(
                    Face.Right,
                    placed.Rotation);

            wheelFace =
                FaceHelper.RotateFace3D(
                    wheelFace,
                    placed.Transform.Rotation);

            bool isSelected =
    selectedParts.Contains(placed);

            Brush outlineBrush =
                isSelected
                    ? Brushes.LimeGreen
                    : Brushes.Black;

            double wheelDiameter =
                wheel.OuterDiameter * Scale;

            double rimDiameter =
                wheel.RimDiameter * Scale;

            double wheelWidth =
                wheel.Width * Scale;

            double halfWidth =
                wheelWidth / 2.0;

            // Gleicher Abstand wie in der 3D-Darstellung:
            // äußere Radseite ungefähr am Ende der Rasterzelle.
            double armEndDistance =
                Grider.CellSize * Scale / 2.0;

            double wheelCenterDistance =
                armEndDistance - halfWidth;

            Vector3 wheelCenter =
                new Vector3(
                    cellCenter.X,
                    cellCenter.Y,
                    cellCenter.Z);

            switch (wheelFace)
            {
                case Face.Right:
                    wheelCenter.X += wheelCenterDistance;
                    break;

                case Face.Left:
                    wheelCenter.X -= wheelCenterDistance;
                    break;

                case Face.Top:
                    wheelCenter.Y -= wheelCenterDistance;
                    break;

                case Face.Bottom:
                    wheelCenter.Y += wheelCenterDistance;
                    break;

                case Face.Front:
                case Face.Back:
                    DrawWheelFromFront(
                        wheelCenter,
                        wheelDiameter,
                        wheel.HoleDiameter * Scale,
                        outlineBrush);

                    return;
            }

            bool horizontalAxis =
                wheelFace == Face.Left ||
                wheelFace == Face.Right;

            // Schwarzer, abgerundeter Gummireifen
            Rectangle tireShape = new Rectangle
            {
                Fill = Brushes.Black,
                Stroke = outlineBrush,
                StrokeThickness = isSelected ? 2.0 : 1.0,
                RadiusX = halfWidth,
                RadiusY = halfWidth
            };

            if (horizontalAxis)
            {
                tireShape.Width = wheelWidth;
                tireShape.Height = wheelDiameter;
            }
            else
            {
                tireShape.Width = wheelDiameter;
                tireShape.Height = wheelWidth;
            }

            Canvas.SetLeft(
                tireShape,
                wheelCenter.X - tireShape.Width / 2.0);

            Canvas.SetTop(
                tireShape,
                wheelCenter.Y - tireShape.Height / 2.0);

            BuildArea.Children.Add(tireShape);

            // Rote Felge innerhalb des Reifens
            double rimWidth =
                wheelWidth * 0.60;

            Rectangle rimShape = new Rectangle
            {
                Fill = Brushes.Red,
                Stroke = Brushes.DarkRed,
                StrokeThickness = 1.0,
                RadiusX = rimWidth / 3.0,
                RadiusY = rimWidth / 3.0
            };

            if (horizontalAxis)
            {
                rimShape.Width = rimWidth;
                rimShape.Height = rimDiameter;
            }
            else
            {
                rimShape.Width = rimDiameter;
                rimShape.Height = rimWidth;
            }

            Canvas.SetLeft(
                rimShape,
                wheelCenter.X - rimShape.Width / 2.0);

            Canvas.SetTop(
                rimShape,
                wheelCenter.Y - rimShape.Height / 2.0);

            BuildArea.Children.Add(rimShape);

            // Bohrung beziehungsweise Achse
            double hubDiameter =
                wheel.HoleDiameter * Scale;

            Rectangle hubShape = new Rectangle
            {
                Fill = Brushes.LightGray,
                Stroke = Brushes.Black,
                StrokeThickness = 1.0,
                RadiusX = 2.0,
                RadiusY = 2.0
            };

            if (horizontalAxis)
            {
                hubShape.Width = wheelWidth + 2.0;
                hubShape.Height = hubDiameter;
            }
            else
            {
                hubShape.Width = hubDiameter;
                hubShape.Height = wheelWidth + 2.0;
            }

            Canvas.SetLeft(
                hubShape,
                wheelCenter.X - hubShape.Width / 2.0);

            Canvas.SetTop(
                hubShape,
                wheelCenter.Y - hubShape.Height / 2.0);

            BuildArea.Children.Add(hubShape);
            
        }

        private void DrawWheelFromFront(
    Vector3 center,
    double outerDiameter,
    double holeDiameter,
    Brush brush)
        {
            Ellipse wheelShape = new Ellipse
            {
                Width = outerDiameter,
                Height = outerDiameter,
                Fill = brush,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };

            Canvas.SetLeft(
                wheelShape,
                center.X - outerDiameter / 2.0);

            Canvas.SetTop(
                wheelShape,
                center.Y - outerDiameter / 2.0);

            BuildArea.Children.Add(wheelShape);

            Ellipse holeShape = new Ellipse
            {
                Width = holeDiameter,
                Height = holeDiameter,
                Fill = Brushes.White,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };

            Canvas.SetLeft(
                holeShape,
                center.X - holeDiameter / 2.0);

            Canvas.SetTop(
                holeShape,
                center.Y - holeDiameter / 2.0);

            BuildArea.Children.Add(holeShape);
        }

        
        private void RedrawWorld()
        {
            worldPartMap.Clear();

            while (WorldViewport.Children.Count > 1)
                WorldViewport.Children.RemoveAt(1);

            
            foreach (PlacedPart placed in assembly.PlacedParts)
            {
                if (placed.Part is Wheel wheel)
                {
                    double wx =
                        (placed.Transform.Position.X / Scale
                        + Grider.CellSize / 2.0) / 100.0;

                    double wy =
                        -(placed.Transform.Position.Y / Scale
                        + Grider.CellSize / 2.0) / 100.0;

                    double wz =
                        placed.Transform.Position.Z / 100.0;

                    Point3D cellCenter =
                        new Point3D(
                            wx,
                            wy,
                            wz);

                    // Das Rad zeigt zunächst nach rechts.
                    // placed.Rotation wählt wie bei einem einarmigen Teil
                    // den gewünschten Arm.
                    Face wheelFace =
                        FaceHelper.RotateFace(
                            Face.Right,
                            placed.Rotation);

                    Vector3 direction =
                        GetDirectionFromFace(wheelFace);

                    direction =
                        placed.Transform.ApplyRotation(direction);

                    double halfWidth =
                        wheel.Width / 200.0;

                    double wheelRadius =
                        wheel.OuterDiameter / 200.0;

                    // Außenkante des Rades liegt ungefähr am Ende des Arms.
                    double armEndDistance =
                        (Grider.CellSize / 2.0) / 100.0;

                    double wheelCenterDistance =
                        armEndDistance - halfWidth;

                    Point3D wheelCenter =
                        new Point3D(
                            cellCenter.X
                                + direction.X * wheelCenterDistance,

                            cellCenter.Y
                                - direction.Y * wheelCenterDistance,

                            cellCenter.Z
                                + direction.Z * wheelCenterDistance);

                    Point3D start =
                        new Point3D(
                            wheelCenter.X
                                - direction.X * halfWidth,

                            wheelCenter.Y
                                + direction.Y * halfWidth,

                            wheelCenter.Z
                                - direction.Z * halfWidth);

                    Point3D end =
                        new Point3D(
                            wheelCenter.X
                                + direction.X * halfWidth,

                            wheelCenter.Y
                                - direction.Y * halfWidth,

                            wheelCenter.Z
                                + direction.Z * halfWidth);

                    // Schwarzer Reifen
                    double tubeRadius =
    wheel.TireThickness / 2.0 / 100.0;

                    double majorRadius =
                        (wheel.OuterDiameter - wheel.TireThickness)
                        / 2.0 / 100.0;

                    Brush tireBrush =
    selectedParts.Contains(placed)
        ? new SolidColorBrush(
            Color.FromRgb(70, 70, 70))
        : Brushes.Black;

                    AddTorus(
                        wheelCenter,
                        direction,
                        majorRadius,
                        tubeRadius,
                        placed,
                        tireBrush);


                    // Rote Felge
                    double rimRadius =
                        wheel.RimDiameter / 200.0;

                    double rimHalfWidth =
                        wheel.Width * 0.35 / 100.0;

                    Point3D rimStart = new Point3D(
                        wheelCenter.X - direction.X * rimHalfWidth,
                        wheelCenter.Y + direction.Y * rimHalfWidth,
                        wheelCenter.Z - direction.Z * rimHalfWidth);

                    Point3D rimEnd = new Point3D(
                        wheelCenter.X + direction.X * rimHalfWidth,
                        wheelCenter.Y - direction.Y * rimHalfWidth,
                        wheelCenter.Z + direction.Z * rimHalfWidth);

                    double rimOuterRadius =
    wheel.RimDiameter / 200.0;

                    double rimHoleRadius =
                        wheel.HoleDiameter / 200.0;

                    rimHalfWidth =
                        wheel.Width * 0.42 / 100.0;
                    
                    Brush rimBrush = PaksyRed;

                    if (selectedParts.Contains(placed))
                    {
                        rimBrush = HighlightBrush(rimBrush);
                    }


                    AddRim(
                        wheelCenter,
                        direction,
                        rimOuterRadius,
                        rimHoleRadius,
                        rimHalfWidth,
                        placed,
                        rimBrush);// Kleine rote Nabe



                    double hubRadius =
                        wheel.HoleDiameter / 200.0;

                    double hubHalfWidth =
                        (wheel.Width + 2.0) / 200.0;

                    Point3D hubStart =
                        new Point3D(
                            wheelCenter.X
                                - direction.X * hubHalfWidth,

                            wheelCenter.Y
                                + direction.Y * hubHalfWidth,

                            wheelCenter.Z
                                - direction.Z * hubHalfWidth);

                    Point3D hubEnd =
                        new Point3D(
                            wheelCenter.X
                                + direction.X * hubHalfWidth,

                            wheelCenter.Y
                                - direction.Y * hubHalfWidth,

                            wheelCenter.Z
                                + direction.Z * hubHalfWidth);

           
                    AddRim(
                        wheelCenter,
                        direction,
                        rimOuterRadius,
                        rimHoleRadius,
                        rimHalfWidth,
                        placed,
                        rimBrush);

                    continue;
                }

                if (placed.Part is EndCap endCap)
                {
                    DrawEndCap3D(
                        placed,
                        endCap);

                    continue;
                }

                if (placed.Part is WindowPlate windowPlate)
                {
                    
                    continue;
                }

                if (placed.Part is Plate plate)
                {
                    DrawPlate3D(
                        placed,
                        plate);

                    continue;
                }

                if (placed.Part is Cube cube)
                {
                    DrawCube3D(
                        placed,
                        cube);

                    continue;
                }
                

                if (placed.Part is BallConnector ball)
                {
                    DrawBallConnector3D(
                        placed,
                        ball);

                    continue;
                }

                

                if (!(placed.Part is StructuralPart part))
                    continue;

                double x =
                    (placed.Transform.Position.X / Scale
                    + Grider.CellSize / 2.0) / 100.0;

                double y =
                    -(placed.Transform.Position.Y / Scale
                    + Grider.CellSize / 2.0) / 100.0;

                double z =
                    placed.Transform.Position.Z / 100.0;

                Point3D center =
                    new Point3D(
                        x,
                        y,
                        z);

                double radius =
                    part.OuterDiameter / 200.0;

                double armLength =
                    (part.Length / 2.0) / 100.0;

                // Kugel im Mittelpunkt für Winkel, T-Stück, Kreuz usw.
                if (part.DrawCenter)
                {
                    AddSphere(
                        center,
                        radius,
                        placed);
                }

                // Ein Rohrarm pro Socket
                foreach (Socket socket in placed.Sockets)
                {
                    Face rotatedFace =
                        FaceHelper.RotateFace(
                            socket.Face,
                            placed.Rotation);

                    Vector3 direction =
                        GetDirectionFromFace(rotatedFace);

                    // zukünftige echte 3D-Rotation zusätzlich anwenden
                    direction =
                        placed.Transform.ApplyRotation(direction);

                    Point3D end =
                        new Point3D(
                            center.X
                                + direction.X * armLength,

                            center.Y
                                - direction.Y * armLength,

                            center.Z
                                + direction.Z * armLength);

                    AddCylinder(
                        center,
                        end,
                        radius,
                        placed);
                }

                // Transparente Fenster immer zuletzt zeichnen
               

                
            }
            foreach (PlacedPart placed2 in assembly.PlacedParts)
            {
                if (placed2.Part is WindowPlate windowPlatew)
                {
                    DrawWindow3D(
                        placed2,
                        windowPlatew);
                }
            }
            if ((isWorldPartDragging || showMoveGrid) &&
    dragGridPlaneY.HasValue &&
    dragGridReferencePart != null)
            {
                ShowDragGrid(
                    dragGridPlaneY.Value,
                    dragGridReferencePart);
            }


            if (!worldCameraInitialized &&
                    assembly.PlacedParts.Count > 0)
            {
                FitWorldCamera();
                worldCameraInitialized = true;
            }
        }
        private void DrawBallConnector3D(
    PlacedPart placed,
    BallConnector ball)
        {
            double x =
                (placed.Transform.Position.X / Scale
                + Grider.CellSize / 2.0) / 100.0;

            double y =
                -(placed.Transform.Position.Y / Scale
                + Grider.CellSize / 2.0) / 100.0;

            double z =
                placed.Transform.Position.Z / 100.0;

            Point3D center =
                new Point3D(
                    x,
                    y,
                    z);

            double ballRadius =
                ball.Diameter / 200.0;

            double holeRadius =
                ball.HoleDiameter / 200.0;

            Brush ballBrush =
                GetWorldPartBrush(placed);

            // Blaue Grundkugel
            AddSphereWithBrush(
                center,
                ballRadius,
                placed,
                ballBrush);

            // Sechs Sacklochöffnungen
            AddBallHole(
                center,
                new Vector3D(-1, 0, 0),
                ballRadius,
                holeRadius,
                placed);

            AddBallHole(
                center,
                new Vector3D(1, 0, 0),
                ballRadius,
                holeRadius,
                placed);

            AddBallHole(
                center,
                new Vector3D(0, -1, 0),
                ballRadius,
                holeRadius,
                placed);

            AddBallHole(
                center,
                new Vector3D(0, 1, 0),
                ballRadius,
                holeRadius,
                placed);

            AddBallHole(
                center,
                new Vector3D(0, 0, -1),
                ballRadius,
                holeRadius,
                placed);

            AddBallHole(
                center,
                new Vector3D(0, 0, 1),
                ballRadius,
                holeRadius,
                placed);
        }
        private void AddBallHole(
    Point3D ballCenter,
    Vector3D outwardDirection,
    double ballRadius,
    double holeRadius,
    PlacedPart placed)
        {
            if (outwardDirection.Length == 0)
                return;

            outwardDirection.Normalize();

            // Minimal außerhalb der Kugeloberfläche,
            // damit die Fläche nicht flimmert.
            double surfaceOffset =
                0.0003;

            Point3D holeCenter =
                ballCenter
                + outwardDirection
                * (ballRadius + surfaceOffset);

            Brush holeEdgeBrush =
                selectedParts.Contains(placed)
                    ? new SolidColorBrush(
                        Color.FromRgb(
                            90,
                            110,
                            120))
                    : new SolidColorBrush(
                        Color.FromRgb(
                            20,
                            40,
                            50));

            Brush holeInsideBrush =
                selectedParts.Contains(placed)
                    ? new SolidColorBrush(
                        Color.FromRgb(
                            45,
                            55,
                            60))
                    : Brushes.Black;

            // Äußerer Lochrand
            AddDisc3D(
                holeCenter,
                outwardDirection,
                holeRadius,
                placed,
                holeEdgeBrush);

            // Dunkler Innenbereich erzeugt den Eindruck
            // eines etwa 10 mm tiefen Sackloches.
            Point3D innerCenter =
                holeCenter
                + outwardDirection * 0.0002;

            AddDisc3D(
                innerCenter,
                outwardDirection,
                holeRadius * 0.72,
                placed,
                holeInsideBrush);
        }
        private void DrawCube3D(
    PlacedPart placed,
    Cube cube)
        {
            double x =
                (placed.Transform.Position.X / Scale
                + Grider.CellSize / 2.0) / 100.0;

            double y =
                -(placed.Transform.Position.Y / Scale
                + Grider.CellSize / 2.0) / 100.0;

            double z =
                placed.Transform.Position.Z / 100.0;

            Point3D center =
                new Point3D(
                    x,
                    y,
                    z);

            double size =
                cube.Size / 100.0;

            double cornerRadius =
                cube.CornerRadius / 100.0;

            double holeRadius =
                cube.HoleDiameter / 200.0;

            Brush cubeBrush =
                GetWorldPartBrush(placed);

            AddRoundedCube(
                center,
                size,
                cornerRadius,
                placed,
                cubeBrush);

            AddCubeHole(
                center,
                new Vector3D(-1, 0, 0),
                size,
                holeRadius,
                placed);

            AddCubeHole(
                center,
                new Vector3D(1, 0, 0),
                size,
                holeRadius,
                placed);

            AddCubeHole(
                center,
                new Vector3D(0, -1, 0),
                size,
                holeRadius,
                placed);

            AddCubeHole(
                center,
                new Vector3D(0, 1, 0),
                size,
                holeRadius,
                placed);

            AddCubeHole(
                center,
                new Vector3D(0, 0, -1),
                size,
                holeRadius,
                placed);

            AddCubeHole(
                center,
                new Vector3D(0, 0, 1),
                size,
                holeRadius,
                placed);
        }

        private void AddRoundedCube(
    Point3D center,
    double size,
    double cornerRadius,
    PlacedPart placed,
    Brush brush)
        {
            double innerSize =
                size - 2.0 * cornerRadius;

            if (innerSize <= 0)
                return;

            double halfInner =
                innerSize / 2.0;

            /*
             * Drei überlappende Quader bilden
             * die geraden Flächen des Würfels.
             */

            AddBox(
                center,
                size,
                innerSize,
                innerSize,
                placed,
                brush);

            AddBox(
                center,
                innerSize,
                size,
                innerSize,
                placed,
                brush);

            AddBox(
                center,
                innerSize,
                innerSize,
                size,
                placed,
                brush);

            /*
             * Kanten parallel zur X-Achse
             */

            foreach (double ySign in new[] { -1.0, 1.0 })
            {
                foreach (double zSign in new[] { -1.0, 1.0 })
                {
                    Point3D start =
                        new Point3D(
                            center.X - halfInner,
                            center.Y + ySign * halfInner,
                            center.Z + zSign * halfInner);

                    Point3D end =
                        new Point3D(
                            center.X + halfInner,
                            center.Y + ySign * halfInner,
                            center.Z + zSign * halfInner);

                    AddCylinder(
                        start,
                        end,
                        cornerRadius,
                        placed,
                        brush);
                }
            }

            /*
             * Kanten parallel zur Y-Achse
             */

            foreach (double xSign in new[] { -1.0, 1.0 })
            {
                foreach (double zSign in new[] { -1.0, 1.0 })
                {
                    Point3D start =
                        new Point3D(
                            center.X + xSign * halfInner,
                            center.Y - halfInner,
                            center.Z + zSign * halfInner);

                    Point3D end =
                        new Point3D(
                            center.X + xSign * halfInner,
                            center.Y + halfInner,
                            center.Z + zSign * halfInner);

                    AddCylinder(
                        start,
                        end,
                        cornerRadius,
                        placed,
                        brush);
                }
            }

            /*
             * Kanten parallel zur Z-Achse
             */

            foreach (double xSign in new[] { -1.0, 1.0 })
            {
                foreach (double ySign in new[] { -1.0, 1.0 })
                {
                    Point3D start =
                        new Point3D(
                            center.X + xSign * halfInner,
                            center.Y + ySign * halfInner,
                            center.Z - halfInner);

                    Point3D end =
                        new Point3D(
                            center.X + xSign * halfInner,
                            center.Y + ySign * halfInner,
                            center.Z + halfInner);

                    AddCylinder(
                        start,
                        end,
                        cornerRadius,
                        placed,
                        brush);
                }
            }

            /*
             * Acht abgerundete Ecken
             */

            foreach (double xSign in new[] { -1.0, 1.0 })
            {
                foreach (double ySign in new[] { -1.0, 1.0 })
                {
                    foreach (double zSign in new[] { -1.0, 1.0 })
                    {
                        Point3D cornerCenter =
                            new Point3D(
                                center.X + xSign * halfInner,
                                center.Y + ySign * halfInner,
                                center.Z + zSign * halfInner);

                        AddSphereWithBrush(
                            cornerCenter,
                            cornerRadius,
                            placed,
                            brush);
                    }
                }
            }
        }

        private void AddSphereWithBrush(
    Point3D center,
    double radius,
    PlacedPart placed,
    Brush brush)
        {
            const int latitudeSegments = 12;
            const int longitudeSegments = 20;

            MeshGeometry3D mesh =
                new MeshGeometry3D();

            for (int latitude = 0;
                 latitude <= latitudeSegments;
                 latitude++)
            {
                double theta =
                    Math.PI
                    * latitude
                    / latitudeSegments;

                double sinTheta =
                    Math.Sin(theta);

                double cosTheta =
                    Math.Cos(theta);

                for (int longitude = 0;
                     longitude <= longitudeSegments;
                     longitude++)
                {
                    double phi =
                        2.0
                        * Math.PI
                        * longitude
                        / longitudeSegments;

                    double x =
                        center.X
                        + radius
                        * sinTheta
                        * Math.Cos(phi);

                    double y =
                        center.Y
                        + radius
                        * cosTheta;

                    double z =
                        center.Z
                        + radius
                        * sinTheta
                        * Math.Sin(phi);

                    mesh.Positions.Add(
                        new Point3D(
                            x,
                            y,
                            z));
                }
            }

            for (int latitude = 0;
                 latitude < latitudeSegments;
                 latitude++)
            {
                for (int longitude = 0;
                     longitude < longitudeSegments;
                     longitude++)
                {
                    int first =
                        latitude
                        * (longitudeSegments + 1)
                        + longitude;

                    int second =
                        first
                        + longitudeSegments
                        + 1;

                    mesh.TriangleIndices.Add(first);
                    mesh.TriangleIndices.Add(second);
                    mesh.TriangleIndices.Add(first + 1);

                    mesh.TriangleIndices.Add(second);
                    mesh.TriangleIndices.Add(second + 1);
                    mesh.TriangleIndices.Add(first + 1);
                }
            }

            DiffuseMaterial material =
                new DiffuseMaterial(brush);

            GeometryModel3D model =
                new GeometryModel3D
                {
                    Geometry = mesh,
                    Material = material,
                    BackMaterial = material
                };

            worldPartMap[model] = placed;

            WorldViewport.Children.Add(
                new ModelVisual3D
                {
                    Content = model
                });
        }

        private void AddCubeHole(
    Point3D cubeCenter,
    Vector3D outwardDirection,
    double cubeSize,
    double holeRadius,
    PlacedPart placed)
        {
            if (outwardDirection.Length == 0)
                return;

            outwardDirection.Normalize();

            double halfSize =
                cubeSize / 2.0;

            // Leicht außerhalb der Oberfläche,
            // damit keine flimmernden Flächen entstehen.
            double surfaceOffset =
                0.0003;

            Point3D holeCenter =
                cubeCenter
                + outwardDirection
                * (halfSize + surfaceOffset);

            Brush outerHoleBrush =
                selectedParts.Contains(placed)
                    ? new SolidColorBrush(
                        Color.FromRgb(
                            80,
                            100,
                            105))
                    : new SolidColorBrush(
                        Color.FromRgb(
                            20,
                            35,
                            45));

            Brush innerHoleBrush =
                selectedParts.Contains(placed)
                    ? new SolidColorBrush(
                        Color.FromRgb(
                            45,
                            55,
                            60))
                    : Brushes.Black;

            AddDisc3D(
                holeCenter,
                outwardDirection,
                holeRadius,
                placed,
                outerHoleBrush);

            // Kleiner dunkler Innenbereich erzeugt
            // den Eindruck eines tiefen Sackloches.
            Point3D innerCenter =
                holeCenter
                + outwardDirection * 0.0002;

            AddDisc3D(
                innerCenter,
                outwardDirection,
                holeRadius * 0.72,
                placed,
                innerHoleBrush);
        }

        private void AddDisc3D(
    Point3D center,
    Vector3D normal,
    double radius,
    PlacedPart placed,
    Brush brush)
        {
            const int segments = 32;

            if (normal.Length == 0)
                return;

            normal.Normalize();

            Vector3D reference =
                Math.Abs(normal.Y) < 0.9
                    ? new Vector3D(0, 1, 0)
                    : new Vector3D(1, 0, 0);

            Vector3D side1 =
                Vector3D.CrossProduct(
                    normal,
                    reference);

            side1.Normalize();

            Vector3D side2 =
                Vector3D.CrossProduct(
                    normal,
                    side1);

            side2.Normalize();

            MeshGeometry3D mesh =
                new MeshGeometry3D();

            int centerIndex =
                mesh.Positions.Count;

            mesh.Positions.Add(center);

            for (int index = 0;
                 index < segments;
                 index++)
            {
                double angle =
                    2.0
                    * Math.PI
                    * index
                    / segments;

                Vector3D offset =
                    side1
                    * (Math.Cos(angle) * radius)
                    + side2
                    * (Math.Sin(angle) * radius);

                mesh.Positions.Add(
                    center + offset);
            }

            for (int index = 0;
                 index < segments;
                 index++)
            {
                int next =
                    (index + 1) % segments;

                mesh.TriangleIndices.Add(
                    centerIndex);

                mesh.TriangleIndices.Add(
                    index + 1);

                mesh.TriangleIndices.Add(
                    next + 1);
            }

            DiffuseMaterial material =
                new DiffuseMaterial(brush);

            GeometryModel3D model =
                new GeometryModel3D
                {
                    Geometry = mesh,
                    Material = material,
                    BackMaterial = material
                };

            worldPartMap[model] = placed;

            WorldViewport.Children.Add(
                new ModelVisual3D
                {
                    Content = model
                });
        }
        private void DrawWindow3D(
    PlacedPart placed,
    WindowPlate windowPlate)
        {
            double x =
                (placed.Transform.Position.X / Scale
                + Grider.CellSize / 2.0) / 100.0;

            double y =
                -(placed.Transform.Position.Y / Scale
                + Grider.CellSize / 2.0) / 100.0;

            double z =
                placed.Transform.Position.Z / 100.0;

            double halfGrid =
                (Grider.CellSize / 2.0) / 100.0;

            double width =
                windowPlate.Width / 100.0;

            double height =
                windowPlate.Height / 100.0;

            double thickness =
                windowPlate.Thickness / 100.0;

            double barWidth =
                windowPlate.CenterBarWidth / 100.0;

            // Der Mittelstrich ist minimal dicker als das Plexiglas,
            // damit er nicht in der Scheibe verschwindet.
            double barThickness =
                thickness + 0.002;

            Brush glassBrush =
                selectedParts.Contains(placed)
                    ? new SolidColorBrush(
                        Color.FromArgb(
                            110,
                            180,
                            255,
                            210))
                    : new SolidColorBrush(
                        Color.FromArgb(
                            45,
                            180,
                            230,
                            255));

            Brush centerBarBrush =
                new SolidColorBrush(
                    Color.FromArgb(
                        90,
                        190,
                        210,
                        220));

            Point3D center;

            switch (placed.PlateOrientation)
            {
                // XY-Ebene
                case 0:
                    center = new Point3D(
                        x + halfGrid,
                        y - halfGrid,
                        z);

                    // Plexiglasscheibe
                    AddBox(
                        center,
                        width,
                        height,
                        thickness,
                        placed,
                        glassBrush);

                    // Dünner Strich von oben nach unten
                    AddBox(
                        center,
                        barWidth,
                        height,
                        barThickness,
                        placed,
                        centerBarBrush);

                    break;

                // XZ-Ebene
                case 1:
                    center = new Point3D(
                        x + halfGrid,
                        y,
                        z + halfGrid);

                    // Plexiglasscheibe
                    AddBox(
                        center,
                        width,
                        thickness,
                        height,
                        placed,
                        glassBrush);

                    // Senkrechter Mittelstrich entlang Z
                    AddBox(
                        center,
                        barWidth,
                        barThickness,
                        height,
                        placed,
                        centerBarBrush);

                    break;

                // YZ-Ebene
                case 2:
                    center = new Point3D(
                        x,
                        y - halfGrid,
                        z + halfGrid);

                    // Plexiglasscheibe
                    AddBox(
                        center,
                        thickness,
                        width,
                        height,
                        placed,
                        glassBrush);

                    // Senkrechter Mittelstrich entlang Z
                    AddBox(
                        center,
                        barThickness,
                        barWidth,
                        height,
                        placed,
                        centerBarBrush);

                    break;

                default:
                    placed.PlateOrientation = 0;
                    return;
            }
        }


        private Brush GetWorldPartBrush(PlacedPart placed)
        {

            SolidColorBrush PartBrush =
    new SolidColorBrush(
        Color.FromRgb(
            35,
            140,
            195));

            return selectedParts.Contains(placed)
                ? Brushes.LimeGreen
                : PartBrush;
        }

        private void FitWorldCamera()
        {
            if (assembly.PlacedParts.Count == 0)
                return;

            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double minZ = double.MaxValue;

            double maxX = double.MinValue;
            double maxY = double.MinValue;
            double maxZ = double.MinValue;

            foreach (PlacedPart placed in assembly.PlacedParts)
            {
                double x =
                    (placed.Transform.Position.X / Scale
                    + Grider.CellSize / 2.0) / 100.0;

                double y =
                    -(placed.Transform.Position.Y / Scale
                    + Grider.CellSize / 2.0) / 100.0;

                double z =
                    placed.Transform.Position.Z / 100.0;

                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);

                minY = Math.Min(minY, y);
                maxY = Math.Max(maxY, y);

                minZ = Math.Min(minZ, z);
                maxZ = Math.Max(maxZ, z);
            }

            double centerX = (minX + maxX) / 2.0;
            double centerY = (minY + maxY) / 2.0;
            double centerZ = (minZ + maxZ) / 2.0;

            double sizeX = maxX - minX;
            double sizeY = maxY - minY;
            double sizeZ = maxZ - minZ;

            double size = Math.Max(
                Math.Max(sizeX, sizeY),
                sizeZ);

            // Auch bei nur einem Bauteil sinnvollen Abstand behalten.
            size = Math.Max(size, 1.0);

            Point3D target =
                new Point3D(
                    centerX,
                    centerY,
                    centerZ);

            WorldCamera.Position =
                new Point3D(
                    centerX + size * 1.5,
                    centerY + size * 1.5,
                    centerZ + size * 2.0);

            WorldCamera.LookDirection =
                target - WorldCamera.Position;

            WorldCamera.UpDirection =
                new Vector3D(0, 1, 0);
        }
        private Vector3 GetDirectionFromFace(Face face)
        {
            switch (face)
            {
                case Face.Left:
                    return new Vector3(-1, 0, 0);

                case Face.Right:
                    return new Vector3(1, 0, 0);

                case Face.Top:
                    return new Vector3(0, -1, 0);

                case Face.Bottom:
                    return new Vector3(0, 1, 0);

                case Face.Front:
                    return new Vector3(0, 0, 1);

                case Face.Back:
                    return new Vector3(0, 0, -1);

                default:
                    return new Vector3(0, 0, 0);
            }
        }
        private void AddSphere(
    Point3D center,
    double radius,
    PlacedPart placed)
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
    new DiffuseMaterial(
        GetWorldPartBrush(placed));

            GeometryModel3D model =
                new GeometryModel3D
                {
                    Geometry = mesh,
                    Material = material,
                    BackMaterial = material
                };
            worldPartMap[model] = placed;

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
                bool isCurrentLayer =
                    Math.Abs(
                        placed.Transform.Position.Z - currentPlanZ)
                    < 0.001;

                if (!isCurrentLayer)
                    continue;

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
    double radius,
    PlacedPart placed,
    Brush brush = null)
        {
            const int segments = 24;

            Vector3D axis = end - start;

            if (axis.Length == 0)
                return;

            axis.Normalize();

            Vector3D reference =
                Math.Abs(axis.Y) < 0.9
                    ? new Vector3D(0, 1, 0)
                    : new Vector3D(1, 0, 0);

            Vector3D side1 =
                Vector3D.CrossProduct(
                    axis,
                    reference);

            side1.Normalize();

            Vector3D side2 =
                Vector3D.CrossProduct(
                    axis,
                    side1);

            side2.Normalize();

            MeshGeometry3D mesh =
                new MeshGeometry3D();

            for (int i = 0; i < segments; i++)
            {
                double angle =
                    2.0 * Math.PI * i / segments;

                Vector3D offset =
                    side1 * (Math.Cos(angle) * radius) +
                    side2 * (Math.Sin(angle) * radius);

                mesh.Positions.Add(start + offset);
                mesh.Positions.Add(end + offset);
            }

            for (int i = 0; i < segments; i++)
            {
                int next =
                    (i + 1) % segments;

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

            Brush cylinderBrush =
                brush ?? GetWorldPartBrush(placed);

            DiffuseMaterial material =
                new DiffuseMaterial(cylinderBrush);

            GeometryModel3D model =
                new GeometryModel3D
                {
                    Geometry = mesh,
                    Material = material,
                    BackMaterial = material
                };

            worldPartMap[model] = placed;

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

            bool isCurrentLayer =
    Math.Abs(
        placed.Transform.Position.Z - currentPlanZ)
    < 0.001;

            Brush brush;

            if (selectedParts.Contains(placed))
            {
                brush = Brushes.LimeGreen;
            }
            else if (isCurrentLayer)
            {
                brush = Brushes.Blue;
            }
            else
            {
                brush = new SolidColorBrush(
                    Color.FromArgb(
                        55,
                        0,
                        0,
                        255));
            }


            if (part.DrawCenter)
            {
                DrawCenter(
                    center,
                    part.OuterDiameter,
                    brush);
            }

            var rotatedFaces = placed.Sockets
    .Select(socket =>
    {
        Face face = FaceHelper.RotateFace(
            socket.Face,
            placed.Rotation);

        return FaceHelper.RotateFace3D(
            face,
            placed.Transform.Rotation);
    })
    .ToList();

            bool hasFrontAndBack =
                rotatedFaces.Contains(Face.Front) &&
                rotatedFaces.Contains(Face.Back);

            bool depthSymbolDrawn = false;
            Face? depthFace = null;
            bool hasFrontArm = false;

            foreach (Socket socket in placed.Sockets)
            {
                Face face =
                    FaceHelper.RotateFace(
                        socket.Face,
                        placed.Rotation);

                face =
                    FaceHelper.RotateFace3D(
                        face,
                        placed.Transform.Rotation);

                if (face == Face.Front)
                {
                    hasFrontArm = true;
                    continue;
                }

                if (face == Face.Back)
                {
                    // Arm zeigt vom Benutzer weg:
                    // in Paksy Plan nichts zusätzlich zeichnen.
                    continue;
                }

                DrawArm(
                    center,
                    face,
                    part.Length / 2,
                    part.OuterDiameter,
                    brush);

                if (isCurrentLayer)
                {
                    DrawSocket(
                        center,
                        face,
                        part.Length / 2,
                        socket.IsConnected);
                }
            }

            if (hasFrontArm && isCurrentLayer)
            {
                DrawDepthSocket(
                    center,
                    part.OuterDiameter);
            }
           
        }

        private void DrawThroughDepthSocket(
    Vector3 center,
    double outerDiameter,
    Brush brush)
        {
            double size = outerDiameter * Scale;

            Ellipse circle = new Ellipse
            {
                Width = size,
                Height = size,
                Stroke = brush,
                StrokeThickness = 2,
                Fill = Brushes.Transparent
            };

            Canvas.SetLeft(
                circle,
                center.X - size / 2);

            Canvas.SetTop(
                circle,
                center.Y - size / 2);

            BuildArea.Children.Add(circle);

            double innerSize = Math.Max(4, size * 0.25);

            Ellipse innerCircle = new Ellipse
            {
                Width = innerSize,
                Height = innerSize,
                Stroke = brush,
                StrokeThickness = 2,
                Fill = Brushes.Transparent
            };

            Canvas.SetLeft(
                innerCircle,
                center.X - innerSize / 2);

            Canvas.SetTop(
                innerCircle,
                center.Y - innerSize / 2);

            BuildArea.Children.Add(innerCircle);
        }
        private void DrawDepthSocket(
    Vector3 center,
    double outerDiameter)
        {
            double size = outerDiameter * Scale;
            double dotSize = Math.Max(5, size * 0.8);

            Ellipse dot = new Ellipse
            {
                Width = dotSize,
                Height = dotSize,
                Fill = Brushes.White,
                Stroke = Brushes.White
            };

            Canvas.SetLeft(
                dot,
                center.X - dotSize / 2);

            Canvas.SetTop(
                dot,
                center.Y - dotSize / 2);

            BuildArea.Children.Add(dot);
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
                    Rotation = part.Rotation,
                    PlateOrientation = part.PlateOrientation
                };

                copy.Transform.Position = new Vector3(
                    part.Transform.Position.X,
                    part.Transform.Position.Y,
                    part.Transform.Position.Z);

                copy.Transform.Rotation = new Vector3(
                    part.Transform.Rotation.X,
                    part.Transform.Rotation.Y,
                    part.Transform.Rotation.Z);

                copy.Transform.Scale = new Vector3(
                    part.Transform.Scale.X,
                    part.Transform.Scale.Y,
                    part.Transform.Scale.Z);

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

                double newZ =
                    source.Transform.Position.Z;

                if (IsPositionOccupied(
     newX,
     newY,
     newZ,
     source.Part))
                {
                    StatusText.Text =
                        "Einfügen nicht möglich: Position ist belegt";

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
                    Rotation = source.Rotation,
                    PlateOrientation = source.PlateOrientation
                };

                pasted.Transform.Position = new Vector3(
                    source.Transform.Position.X + offsetX,
                    source.Transform.Position.Y + offsetY,
                    source.Transform.Position.Z);

                pasted.Transform.Rotation = new Vector3(
                    source.Transform.Rotation.X,
                    source.Transform.Rotation.Y,
                    source.Transform.Rotation.Z);

                pasted.Transform.Scale = new Vector3(
                    source.Transform.Scale.X,
                    source.Transform.Scale.Y,
                    source.Transform.Scale.Z);

                pasted.Sockets = source.Part.CreateSockets();

                assembly.PlacedParts.Add(pasted);
                selectedParts.Add(pasted);

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

                if (part.Part is Plate)
                {
                    part.PlateOrientation =
                        (part.PlateOrientation + 1) % 3;

                    continue;
                }
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
                    
                    PlateOrientation = placed.PlateOrientation,

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
                    Rotation = data.Rotation,
                    PlateOrientation = data.PlateOrientation
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

            worldCameraInitialized = false;

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
                    Rotation = data.Rotation,
                    PlateOrientation = data.PlateOrientation
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


        private void WorldViewport_MouseWheel(
    object sender,
    MouseWheelEventArgs e)
        {
            bool ctrlPressed =
                Keyboard.Modifiers.HasFlag(
                    ModifierKeys.Control);

            if (ctrlPressed)
            {
                // Bisheriger Zoom bleibt exakt erhalten
                Point3D target =
                    WorldCamera.Position + WorldCamera.LookDirection;

                double factor =
                    e.Delta > 0 ? 0.85 : 1.15;

                Vector3D newLookDirection =
                    WorldCamera.LookDirection * factor;

                // Nicht durch das Ziel hindurch zoomen.
                if (newLookDirection.Length < 0.3)
                    return;

                WorldCamera.Position =
                    target - newLookDirection;

                WorldCamera.LookDirection =
                    newLookDirection;

                e.Handled = true;
                return;
            }

            // Ohne Strg: ausgewählte Teile entlang Y verschieben
            if (selectedParts.Count == 0)
                return;

            SaveUndoState();

            foreach (PlacedPart placed in selectedParts)
            {
                DisconnectPart(placed);
            }

            double step =
                Grider.CellSize * Scale;

            double deltaY =
                e.Delta > 0
                    ? -step
                    : step;

            foreach (PlacedPart placed in selectedParts)
            {
                placed.Transform.Position.Y += deltaY;
            }
            if (selectedParts.Count > 0)
            {
                PlacedPart referencePart =
                    selectedParts[0];

                dragGridReferencePart =
                    referencePart;

                dragGridPlaneY =
                    -(
                        referencePart.Transform.Position.Y / Scale
                        + Grider.CellSize / 2.0
                     ) / 100.0;

                showMoveGrid = true;
            }
            int connectionCount =
                ConnectSelectedParts();

            StatusText.Text =
                connectionCount > 0
                    ? $"{connectionCount} Verbindung(en)"
                    : $"{selectedParts.Count} Bauteil(e) verschoben";

            RedrawScene();

            e.Handled = true;
        }

        private void WorldViewport_MouseRightButtonDown(
    object sender,
    MouseButtonEventArgs e)
        {
            if (!Keyboard.Modifiers.HasFlag(
        ModifierKeys.Control))
            {
                return;
            }
            isWorldOrbiting = true;
            worldLastMousePosition = e.GetPosition(WorldViewport);

            Mouse.Capture((IInputElement)sender);

            e.Handled = true;
        }

        private void WorldViewport_MouseRightButtonUp(
            object sender,
            MouseButtonEventArgs e)
        {
            isWorldOrbiting = false;

            Mouse.Capture(null);

            e.Handled = true;
        }

        private void WorldViewport_MouseMove(
    object sender,
    MouseEventArgs e)
        {
            bool ctrlPressed =
                Keyboard.Modifiers.HasFlag(
                    ModifierKeys.Control);

            // ------------------------------------------------------------
            // BAUTEILE VERSCHIEBEN
            // Ohne Strg + linke Maustaste
            // ------------------------------------------------------------
            if (isWorldPartDragging &&
                !ctrlPressed &&
                e.LeftButton == MouseButtonState.Pressed)
            {
                Point current =
                    e.GetPosition(WorldViewport);

                double mouseDeltaX =
                    current.X - worldPartDragStartMouse.X;

                double mouseDeltaY =
                    current.Y - worldPartDragStartMouse.Y;

                if (Math.Abs(mouseDeltaX) > 3 ||
                    Math.Abs(mouseDeltaY) > 3)
                {
                    worldPartWasDragged = true;
                }

                if (!worldPartDragStartPoint.HasValue)
                    return;

                if (selectedParts.Count == 0)
                    return;

                // Wir benutzen das erste ausgewählte Bauteil
                // als Referenz für die X/Z-Arbeitsebene.
                PlacedPart referencePart =
                    selectedParts[0];

                // World-Y entspricht bei unserer Darstellung
                // der Paksy-Y-Position.
                double planeY =
                    -(
                        referencePart.Transform.Position.Y / Scale
                        + Grider.CellSize / 2.0
                     ) / 100.0;

                Point3D? currentPoint =
                    GetMousePointOnWorldPlane(
                        current,
                        planeY);

                if (!currentPoint.HasValue)
                    return;

                Vector3D worldDelta =
                    currentPoint.Value -
                    worldPartDragStartPoint.Value;

                // World-Koordinaten wieder auf Paksy-Koordinaten abbilden.
                double rawDeltaX =
                    worldDelta.X * 100.0 * Scale;

                double rawDeltaZ =
                    worldDelta.Z * 100.0;

                double gridX =
                    Grider.CellSize * Scale;

                double gridZ =
                    Grider.CellSize;

                // Rasterung
                double deltaX =
                    Math.Round(rawDeltaX / gridX)
                    * gridX;

                double deltaZ =
                    Math.Round(rawDeltaZ / gridZ)
                    * gridZ;

                foreach (PlacedPart placed in selectedParts)
                {
                    if (!worldPartDragStartPositions.TryGetValue(
                        placed,
                        out Vector3 start))
                    {
                        continue;
                    }

                    placed.Transform.Position.X =
                        start.X + deltaX;

                    placed.Transform.Position.Z =
                        start.Z + deltaZ;
                }

                RedrawScene();
                return;
            }

            // ------------------------------------------------------------
            // KAMERA
            // Nur mit Strg
            // ------------------------------------------------------------
            if (!ctrlPressed)
                return;

            if (!isWorldOrbiting &&
                !isWorldPanning)
            {
                return;
            }

            Point cameraCurrent =
                e.GetPosition(WorldViewport);

            double cameraDeltaX =
                cameraCurrent.X -
                worldLastMousePosition.X;

            double cameraDeltaY =
                cameraCurrent.Y -
                worldLastMousePosition.Y;

            worldLastMousePosition =
                cameraCurrent;

            // Kamera drehen
            if (isWorldOrbiting)
            {
                OrbitWorldCamera(
                    cameraDeltaX,
                    cameraDeltaY);

                return;
            }

            // Kamera verschieben
            if (isWorldPanning)
            {
                PanWorldCamera(
                    cameraDeltaX,
                    cameraDeltaY);
            }
        }
        private void PanWorldCamera(
    double deltaX,
    double deltaY)
        {
            Vector3D look =
                WorldCamera.LookDirection;

            double distance = look.Length;

            if (distance == 0)
                return;

            look.Normalize();

            Vector3D up =
                WorldCamera.UpDirection;

            up.Normalize();

            Vector3D right =
                Vector3D.CrossProduct(look, up);

            if (right.Length == 0)
                return;

            right.Normalize();

            double speed =
                distance * 0.0015;

            Vector3D movement =
                right * (-deltaX * speed) +
                up * (deltaY * speed);

            WorldCamera.Position += movement;
        }
        private void OrbitWorldCamera(
    double deltaX,
    double deltaY)
        {
            Point3D target =
                WorldCamera.Position +
                WorldCamera.LookDirection;

            Vector3D cameraOffset =
                WorldCamera.Position - target;

            Vector3D up =
                WorldCamera.UpDirection;

            up.Normalize();

            Vector3D right =
                Vector3D.CrossProduct(
                    WorldCamera.LookDirection,
                    up);

            if (right.Length == 0)
                return;

            right.Normalize();

            double horizontalAngle =
                -deltaX * 0.4;

            double verticalAngle =
                -deltaY * 0.4;

            Quaternion horizontalRotation =
                new Quaternion(
                    up,
                    horizontalAngle);

            Quaternion verticalRotation =
                new Quaternion(
                    right,
                    verticalAngle);

            Matrix3D matrix =
                Matrix3D.Identity;

            matrix.Rotate(horizontalRotation);
            matrix.Rotate(verticalRotation);

            cameraOffset =
                matrix.Transform(cameraOffset);

            Vector3D newUp =
                matrix.Transform(up);

            WorldCamera.Position =
                target + cameraOffset;

            WorldCamera.LookDirection =
                target - WorldCamera.Position;

            WorldCamera.UpDirection =
                newUp;
        }

        private void WorldViewport_MouseDown(
    object sender,
    MouseButtonEventArgs e)
        {
            bool ctrlPressed =
    Keyboard.Modifiers.HasFlag(
        ModifierKeys.Control);
            if (e.ChangedButton == MouseButton.Left)
            {
                Point mousePosition =
                    e.GetPosition(WorldViewport);

                HitTestResult result =
                    VisualTreeHelper.HitTest(
                        WorldViewport,
                        mousePosition);

                RayMeshGeometry3DHitTestResult hit =
                    result as RayMeshGeometry3DHitTestResult;

                worldMouseDownPart = null;
                worldPartWasDragged = false;

                if (hit != null &&
                    hit.ModelHit != null &&
                    worldPartMap.TryGetValue(
                        hit.ModelHit,
                        out PlacedPart placed))
                {
                    worldMouseDownPart = placed;

                    // Wenn das Teil bereits zur Auswahl gehört,
                    // bewegen wir die komplette vorhandene Auswahl.
                    //
                    // Ist es NICHT ausgewählt, wird es erst bei
                    // MouseUp zur neuen Auswahl.
                    if (selectedParts.Contains(placed))
                    {
                        SaveUndoState();

                        foreach (PlacedPart part in selectedParts)
                            DisconnectPart(part);

                        worldPartDragStartMouse = mousePosition;

                        PlacedPart referencePart =
    selectedParts[0];

                        double planeY =
                            -(
                                referencePart.Transform.Position.Y / Scale
                                + Grider.CellSize / 2.0
                             ) / 100.0;

                        worldPartDragStartPoint =
                            GetMousePointOnWorldPlane(
                                mousePosition,
                                planeY);

                        worldPartDragStartPositions.Clear();

                        foreach (PlacedPart part in selectedParts)
                        {
                            worldPartDragStartPositions[part] =
                                new Vector3(
                                    part.Transform.Position.X,
                                    part.Transform.Position.Y,
                                    part.Transform.Position.Z);
                        }

                        isWorldPartDragging = true;

                        dragGridPlaneY = planeY;
                        dragGridReferencePart = referencePart;
                        
                        Mouse.Capture((IInputElement)sender);
                    }

                    e.Handled = true;
                }

                return;
            }

            if (e.ChangedButton != MouseButton.Middle)
                return;
            if (!ctrlPressed)
                return;
            isWorldPanning = true;

            worldLastMousePosition =
                e.GetPosition(WorldViewport);

            Mouse.Capture((IInputElement)sender);

            e.Handled = true;
        }

        private void WorldViewport_MouseUp(
            object sender,
            MouseButtonEventArgs e)
        {
            bool ctrlPressed =
    Keyboard.Modifiers.HasFlag(
        ModifierKeys.Control);
            if (e.ChangedButton == MouseButton.Left)
            {
                if (isWorldPartDragging)
                {
                   isWorldPartDragging = false;
                   Mouse.Capture(null);

                    if (worldPartWasDragged)
                    {
                        int connectionCount =
                            ConnectSelectedParts();

                        StatusText.Text =
                            connectionCount > 0
                                ? $"{connectionCount} Verbindung(en)"
                                : $"{selectedParts.Count} Bauteil(e) verschoben";
                    }
                }

                // Kein Drag -> normaler Klick
                if (!worldPartWasDragged &&
                    worldMouseDownPart != null)
                {


                    if (ctrlPressed)
                    {
                        if (selectedParts.Contains(worldMouseDownPart))
                            selectedParts.Remove(worldMouseDownPart);
                        else
                            selectedParts.Add(worldMouseDownPart);
                    }
                    else
                    {
                        selectedParts.Clear();
                        selectedParts.Add(worldMouseDownPart);
                    }

                    StatusText.Text =
                        $"{selectedParts.Count} Bauteil(e) ausgewählt";
                }

                worldPartDragStartPositions.Clear();
                worldMouseDownPart = null;
                worldPartWasDragged = false;

                RedrawScene();

                e.Handled = true;
                return;
            }

            if (e.ChangedButton != MouseButton.Middle)
                return;

            isWorldPanning = false;

            Mouse.Capture(null);

            e.Handled = true;
        }


        private void MainWindow_PreviewKeyDown(
     object sender,
     KeyEventArgs e)
        {

            double moveStep = Grider.CellSize * Scale;

            if (e.Key == Key.A ||
                e.Key == Key.D ||
                e.Key == Key.W ||
                e.Key == Key.S)
            {
                if (selectedParts.Count == 0)
                    return;

                SaveUndoState();

                // Alte Verbindungen lösen
                foreach (PlacedPart placed in selectedParts)
                {
                    DisconnectPart(placed);
                }

                double deltaX = 0;
                double deltaY = 0;

                if (e.Key == Key.A)
                    deltaX = -moveStep;

                if (e.Key == Key.D)
                    deltaX = moveStep;

                if (e.Key == Key.W)
                    deltaY = -moveStep;

                if (e.Key == Key.S)
                    deltaY = moveStep;

                // Gesamte Auswahl gemeinsam bewegen
                foreach (PlacedPart placed in selectedParts)
                {
                    placed.Transform.Position.X += deltaX;
                    placed.Transform.Position.Y += deltaY;
                }

                // Neue räumliche Verbindungen suchen
                int connectionCount = ConnectSelectedParts();

                StatusText.Text =
                    connectionCount > 0
                        ? $"{connectionCount} Verbindung(en)"
                        : $"{selectedParts.Count} Bauteil(e) verschoben";

                RedrawScene();

                e.Handled = true;
                return;
            }

            if (e.Key == Key.PageUp)
            {
                currentPlanZ += Grider.CellSize;

                StatusText.Text =
                    $"Bearbeitungsebene Z = {currentPlanZ:0.##} mm";

                RedrawScene();

                e.Handled = true;
                return;
            }

            if (e.Key == Key.PageDown)
            {
                currentPlanZ -= Grider.CellSize;

                StatusText.Text =
                    $"Bearbeitungsebene Z = {currentPlanZ:0.##} mm";

                RedrawScene();

                e.Handled = true;
                return;
            }

            if (selectedParts.Count == 0)
                return;

            if (e.Key == Key.X)
            {
                SaveUndoState();

                foreach (PlacedPart placed in selectedParts)
                {
                    DisconnectPart(placed);
                }

                foreach (PlacedPart placed in selectedParts)
                {
                    placed.Transform.RotateX90();
                }

                int connectionCount = ConnectSelectedParts();

                StatusText.Text = connectionCount > 0
                    ? $"{connectionCount} Verbindung(en)"
                    : $"{selectedParts.Count} Bauteil(e) um X gedreht";

                RedrawScene();

                e.Handled = true;
            }
            else if (e.Key == Key.Y)
            {
                SaveUndoState();

                foreach (PlacedPart placed in selectedParts)
                {
                    DisconnectPart(placed);
                }

                foreach (PlacedPart placed in selectedParts)
                {
                    placed.Transform.RotateY90();
                }

                int connectionCount = ConnectSelectedParts();

                StatusText.Text = connectionCount > 0
                    ? $"{connectionCount} Verbindung(en)"
                    : $"{selectedParts.Count} Bauteil(e) um Y gedreht";

                RedrawScene();

                e.Handled = true;
            }
            else if (e.Key == Key.Z)
            {
                SaveUndoState();

                foreach (PlacedPart placed in selectedParts)
                {
                    DisconnectPart(placed);
                }

                foreach (PlacedPart placed in selectedParts)
                {
                    placed.Transform.RotateZ90();
                }

                int connectionCount = ConnectSelectedParts();

                StatusText.Text = connectionCount > 0
                    ? $"{connectionCount} Verbindung(en)"
                    : $"{selectedParts.Count} Bauteil(e) um Z gedreht";

                RedrawScene();

                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                SaveUndoState();

                foreach (PlacedPart placed in selectedParts)
                {
                    placed.Transform.Position.Z += Grider.CellSize;
                }

                RedrawScene();

                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                SaveUndoState();

                foreach (PlacedPart placed in selectedParts)
                {
                    placed.Transform.Position.Z -= Grider.CellSize;
                }

                RedrawScene();

                e.Handled = true;
            }
        }

        private Point3D? GetMousePointOnWorldPlane(
    Point mousePosition,
    double planeY)
        {
            if (!(WorldCamera is PerspectiveCamera camera))
                return null;

            double width = WorldViewport.ActualWidth;
            double height = WorldViewport.ActualHeight;

            if (width <= 0 || height <= 0)
                return null;

            // Normalisierte Bildschirmkoordinaten
            double x =
                (2.0 * mousePosition.X / width) - 1.0;

            double y =
                1.0 - (2.0 * mousePosition.Y / height);

            Vector3D forward =
                camera.LookDirection;

            forward.Normalize();

            Vector3D up =
                camera.UpDirection;

            up.Normalize();

            Vector3D right =
                Vector3D.CrossProduct(
                    forward,
                    up);

            right.Normalize();

            // Up nochmals orthogonal machen
            up =
                Vector3D.CrossProduct(
                    right,
                    forward);

            up.Normalize();

            double aspect =
                width / height;

            double tan =
                Math.Tan(
                    camera.FieldOfView *
                    Math.PI /
                    360.0);

            Vector3D rayDirection =
                forward +
                right * (x * tan * aspect) +
                up * (y * tan);

            rayDirection.Normalize();

            // Schnitt mit Ebene Y = planeY
            if (Math.Abs(rayDirection.Y) < 0.000001)
                return null;

            double t =
                (planeY - camera.Position.Y)
                / rayDirection.Y;

            if (t < 0)
                return null;

            return camera.Position +
                rayDirection * t;
        }


        private void AddTorus(
    Point3D center,
    Vector3 direction,
    double majorRadius,
    double tubeRadius,
    PlacedPart placed,
    Brush brush)
        {
            const int majorSegments = 32;
            const int tubeSegments = 16;

            Vector3D axis =
                new Vector3D(
                    direction.X,
                    -direction.Y,
                    direction.Z);

            if (axis.Length == 0)
                return;

            axis.Normalize();

            Vector3D reference =
                Math.Abs(axis.Y) < 0.9
                    ? new Vector3D(0, 1, 0)
                    : new Vector3D(1, 0, 0);

            Vector3D side1 =
                Vector3D.CrossProduct(
                    axis,
                    reference);

            side1.Normalize();

            Vector3D side2 =
                Vector3D.CrossProduct(
                    axis,
                    side1);

            side2.Normalize();

            MeshGeometry3D mesh =
                new MeshGeometry3D();

            for (int majorIndex = 0;
                 majorIndex < majorSegments;
                 majorIndex++)
            {
                double majorAngle =
                    2.0 * Math.PI
                    * majorIndex
                    / majorSegments;

                Vector3D radialDirection =
                    side1 * Math.Cos(majorAngle) +
                    side2 * Math.Sin(majorAngle);

                Point3D ringCenter =
                    center +
                    radialDirection * majorRadius;

                for (int tubeIndex = 0;
                     tubeIndex < tubeSegments;
                     tubeIndex++)
                {
                    double tubeAngle =
                        2.0 * Math.PI
                        * tubeIndex
                        / tubeSegments;

                    Vector3D offset =
                        radialDirection
                            * (Math.Cos(tubeAngle) * tubeRadius)
                        + axis
                            * (Math.Sin(tubeAngle) * tubeRadius);

                    mesh.Positions.Add(
                        ringCenter + offset);
                }
            }

            for (int majorIndex = 0;
                 majorIndex < majorSegments;
                 majorIndex++)
            {
                int nextMajor =
                    (majorIndex + 1)
                    % majorSegments;

                for (int tubeIndex = 0;
                     tubeIndex < tubeSegments;
                     tubeIndex++)
                {
                    int nextTube =
                        (tubeIndex + 1)
                        % tubeSegments;

                    int a =
                        majorIndex * tubeSegments
                        + tubeIndex;

                    int b =
                        nextMajor * tubeSegments
                        + tubeIndex;

                    int c =
                        majorIndex * tubeSegments
                        + nextTube;

                    int d =
                        nextMajor * tubeSegments
                        + nextTube;

                    mesh.TriangleIndices.Add(a);
                    mesh.TriangleIndices.Add(b);
                    mesh.TriangleIndices.Add(c);

                    mesh.TriangleIndices.Add(c);
                    mesh.TriangleIndices.Add(b);
                    mesh.TriangleIndices.Add(d);
                }
            }

            DiffuseMaterial material =
                new DiffuseMaterial(brush);

            GeometryModel3D model =
                new GeometryModel3D
                {
                    Geometry = mesh,
                    Material = material,
                    BackMaterial = material
                };

            worldPartMap[model] = placed;

            WorldViewport.Children.Add(
                new ModelVisual3D
                {
                    Content = model
                });
        }



        private void AddRim(
    Point3D center,
    Vector3 direction,
    double outerRadius,
    double holeRadius,
    double halfWidth,
    PlacedPart placed,
    Brush brush)
        {
            const int segments = 48;

            Vector3D axis =
                new Vector3D(
                    direction.X,
                    -direction.Y,
                    direction.Z);

            if (axis.Length == 0)
                return;

            if (holeRadius <= 0 ||
                outerRadius <= holeRadius ||
                halfWidth <= 0)
            {
                return;
            }

            axis.Normalize();

            Vector3D reference =
                Math.Abs(axis.Y) < 0.9
                    ? new Vector3D(0, 1, 0)
                    : new Vector3D(1, 0, 0);

            Vector3D side1 =
                Vector3D.CrossProduct(
                    axis,
                    reference);

            side1.Normalize();

            Vector3D side2 =
                Vector3D.CrossProduct(
                    axis,
                    side1);

            side2.Normalize();

            /*
             * Felgenprofil:
             *
             * X = Position entlang der Radachse
             * Y = Abstand von der Radachse
             *
             * Das Profil wird anschließend einmal
             * vollständig um die Radachse gedreht.
             */
            Point[] profile =
            {
        // Innere Seite an der Bohrung
        new Point(-halfWidth * 0.55, holeRadius),

        // Vorderer Nabenkörper
        new Point(-halfWidth, holeRadius * 1.30),

        // Felgenschüssel nach außen
        new Point(-halfWidth * 0.75, outerRadius * 0.72),

        // Vorderer Felgenrand
        new Point(-halfWidth * 0.55, outerRadius),

        // Äußerer Felgenkörper
        new Point( halfWidth * 0.55, outerRadius),

        // Hintere Felgenschüssel
        new Point( halfWidth * 0.75, outerRadius * 0.72),

        // Hinterer Nabenkörper
        new Point( halfWidth, holeRadius * 1.30),

        // Innere Seite der Bohrung
        new Point( halfWidth * 0.55, holeRadius)
    };

            MeshGeometry3D mesh =
                new MeshGeometry3D();

            int profileCount =
                profile.Length;

            // Punkte erzeugen
            for (int segment = 0;
                 segment < segments;
                 segment++)
            {
                double angle =
                    2.0 * Math.PI
                    * segment
                    / segments;

                Vector3D radialDirection =
                    side1 * Math.Cos(angle) +
                    side2 * Math.Sin(angle);

                foreach (Point profilePoint in profile)
                {
                    double axialPosition =
                        profilePoint.X;

                    double radialPosition =
                        profilePoint.Y;

                    Point3D point =
                        center
                        + axis * axialPosition
                        + radialDirection * radialPosition;

                    mesh.Positions.Add(point);
                }
            }

            // Flächen zwischen den Profilringen erzeugen
            for (int segment = 0;
                 segment < segments;
                 segment++)
            {
                int nextSegment =
                    (segment + 1) % segments;

                for (int profileIndex = 0;
                     profileIndex < profileCount;
                     profileIndex++)
                {
                    int nextProfile =
                        (profileIndex + 1)
                        % profileCount;

                    int a =
                        segment * profileCount
                        + profileIndex;

                    int b =
                        nextSegment * profileCount
                        + profileIndex;

                    int c =
                        segment * profileCount
                        + nextProfile;

                    int d =
                        nextSegment * profileCount
                        + nextProfile;

                    mesh.TriangleIndices.Add(a);
                    mesh.TriangleIndices.Add(b);
                    mesh.TriangleIndices.Add(c);

                    mesh.TriangleIndices.Add(c);
                    mesh.TriangleIndices.Add(b);
                    mesh.TriangleIndices.Add(d);
                }
            }

            DiffuseMaterial material =
                new DiffuseMaterial(brush);

            GeometryModel3D model =
                new GeometryModel3D
                {
                    Geometry = mesh,
                    Material = material,
                    BackMaterial = material
                };

            worldPartMap[model] = placed;

            WorldViewport.Children.Add(
                new ModelVisual3D
                {
                    Content = model
                });
        }

        private void AddEndCap(
    Point3D center,
    Vector3 direction,
    PlacedPart placed)
        {
            Vector3D axis =
                new Vector3D(
                    direction.X,
                    -direction.Y,
                    direction.Z);

            axis.Normalize();

            // Maße (Meter)
            double flangeRadius = 0.060;
            double flangeLength = 0.010;

            // Kegelansatz genauso groß wie der gelbe Kreis
            double coneRadius = flangeRadius;
            double coneLength = 0.035;

            Point3D flangeStart =
                center;

            Point3D flangeEnd =
                center + axis * flangeLength;

            Brush capBrush = Brushes.Gold;

            if (selectedParts.Contains(placed))
            {
                capBrush = HighlightBrush(capBrush);
            }

            AddCylinder(
                flangeStart,
                flangeEnd,
                flangeRadius,
                placed,
                capBrush);

            AddCone(
                flangeEnd,
                flangeEnd + axis * coneLength,
                coneRadius,
                placed,
                capBrush);
        }

        private void AddCone(
    Point3D start,
    Point3D tip,
    double radius,
    PlacedPart placed,
    Brush brush)
        {
            const int segments = 32;

            Vector3D axis = tip - start;

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
                    2.0 * Math.PI * i / segments;

                Vector3D offset =
                    side1 * Math.Cos(angle) * radius +
                    side2 * Math.Sin(angle) * radius;

                mesh.Positions.Add(start + offset);
            }

            int tipIndex =
                mesh.Positions.Count;

            mesh.Positions.Add(tip);

            for (int i = 0; i < segments; i++)
            {
                int next =
                    (i + 1) % segments;

                mesh.TriangleIndices.Add(i);
                mesh.TriangleIndices.Add(next);
                mesh.TriangleIndices.Add(tipIndex);
            }

            DiffuseMaterial material =
                new DiffuseMaterial(brush);

            GeometryModel3D model =
                new GeometryModel3D
                {
                    Geometry = mesh,
                    Material = material,
                    BackMaterial = material
                };

            worldPartMap[model] = placed;

            WorldViewport.Children.Add(
                new ModelVisual3D
                {
                    Content = model
                });
        }

        private void DrawEndCap3D(
    PlacedPart placed,
    EndCap endCap)
        {
            double wx =
                (placed.Transform.Position.X / Scale
                + Grider.CellSize / 2.0) / 100.0;

            double wy =
                -(placed.Transform.Position.Y / Scale
                + Grider.CellSize / 2.0) / 100.0;

            double wz =
                placed.Transform.Position.Z / 100.0;

            Point3D cellCenter =
                new Point3D(
                    wx,
                    wy,
                    wz);

            Face capFace =
                FaceHelper.RotateFace(
                    Face.Right,
                    placed.Rotation);

            Vector3 direction =
                GetDirectionFromFace(capFace);

            direction =
                placed.Transform.ApplyRotation(direction);

            double halfLength =
                endCap.Length / 200.0;

            double armEndDistance =
                (Grider.CellSize / 2.0) / 100.0;

            double centerDistance =
                armEndDistance;// + halfLength;

            Point3D capCenter =
                new Point3D(
                    cellCenter.X
                        + direction.X * centerDistance,

                    cellCenter.Y
                        - direction.Y * centerDistance,

                    cellCenter.Z
                        + direction.Z * centerDistance);

            AddEndCap(
                    capCenter,
                    direction,
                    placed);
        }

        private void AddBox(
    Point3D center,
    double width,
    double height,
    double depth,
    PlacedPart placed,
    Brush brush)
        {
            double hx = width / 2.0;
            double hy = height / 2.0;
            double hz = depth / 2.0;

            MeshGeometry3D mesh =
                new MeshGeometry3D();

            mesh.Positions.Add(new Point3D(center.X - hx, center.Y - hy, center.Z - hz));
            mesh.Positions.Add(new Point3D(center.X + hx, center.Y - hy, center.Z - hz));
            mesh.Positions.Add(new Point3D(center.X + hx, center.Y + hy, center.Z - hz));
            mesh.Positions.Add(new Point3D(center.X - hx, center.Y + hy, center.Z - hz));

            mesh.Positions.Add(new Point3D(center.X - hx, center.Y - hy, center.Z + hz));
            mesh.Positions.Add(new Point3D(center.X + hx, center.Y - hy, center.Z + hz));
            mesh.Positions.Add(new Point3D(center.X + hx, center.Y + hy, center.Z + hz));
            mesh.Positions.Add(new Point3D(center.X - hx, center.Y + hy, center.Z + hz));

            int[] triangles =
            {
        0,1,2, 0,2,3,
        4,6,5, 4,7,6,
        0,4,5, 0,5,1,
        1,5,6, 1,6,2,
        2,6,7, 2,7,3,
        3,7,4, 3,4,0
    };

            foreach (int i in triangles)
                mesh.TriangleIndices.Add(i);

            DiffuseMaterial material =
                new DiffuseMaterial(brush);

            GeometryModel3D model =
                new GeometryModel3D
                {
                    Geometry = mesh,
                    Material = material,
                    BackMaterial = material
                };

            worldPartMap[model] = placed;

            WorldViewport.Children.Add(
                new ModelVisual3D
                {
                    Content = model
                });
        }

        private void DrawPlate3D(
    PlacedPart placed,
    Plate plate)
        {
            double x =
                (placed.Transform.Position.X / Scale
                + Grider.CellSize / 2.0) / 100.0;

            double y =
                -(placed.Transform.Position.Y / Scale
                + Grider.CellSize / 2.0) / 100.0;

            double z =
                placed.Transform.Position.Z / 100.0;

            double halfGrid =
                (Grider.CellSize / 2.0) / 100.0;

            double width =
                plate.Width / 100.0;

            double height =
                plate.Height / 100.0;

            double thickness =
                plate.Thickness / 100.0;

            Point3D center;

            switch (placed.PlateOrientation)
            {
                // XY-Ebene:
                // zwischen vier Rasterpunkten in X- und Y-Richtung
                case 0:
                    center = new Point3D(
                        x + halfGrid,
                        y - halfGrid,
                        z);

                    Brush plateBrush = PaksyRed;

                    if (selectedParts.Contains(placed))
                    {
                        plateBrush = HighlightBrush(plateBrush);
                    }

                    AddBox(
                        center,
                        width,
                        height,
                        thickness,
                        placed,
                        plateBrush);
                    break;

                // XZ-Ebene:
                // zwischen vier Rasterpunkten in X- und Z-Richtung
                case 1:
                    center = new Point3D(
                        x + halfGrid,
                        y,
                        z + halfGrid);

                    AddBox(
                        center,
                        width,
                        thickness,
                        height,
                        placed,
                        PaksyRed);
                    break;

                // YZ-Ebene:
                // zwischen vier Rasterpunkten in Y- und Z-Richtung
                case 2:
                    center = new Point3D(
                        x,
                        y - halfGrid,
                        z + halfGrid);

                    AddBox(
                        center,
                        thickness,
                        width,
                        height,
                        placed,
                        PaksyRed);
                    break;

                default:
                    placed.PlateOrientation = 0;
                    return;
            }
        }


        private Brush HighlightBrush(
    Brush brush)
        {
            if (brush is SolidColorBrush solid)
            {
                Color c = solid.Color;

                return new SolidColorBrush(
                    Color.FromRgb(
                        (byte)Math.Min(255, c.R + 60),
                        (byte)Math.Min(255, c.G + 60),
                        (byte)Math.Min(255, c.B + 60)));
            }

            return brush;
        }
        
        private void ShowDragGrid(
    double planeY,
    PlacedPart referencePart)
        {
            dragGridVisual = null;
            dragGridPlaneY = planeY;

            double cellSize =
                Grider.CellSize / 100.0;

            double referenceX =
                (referencePart.Transform.Position.X / Scale
                + Grider.CellSize / 2.0) / 100.0;

            double referenceZ =
                referencePart.Transform.Position.Z / 100.0;

            const int cellRadius = 6;

            double minX =
                referenceX - cellRadius * cellSize;

            double maxX =
                referenceX + cellRadius * cellSize;

            double minZ =
                referenceZ - cellRadius * cellSize;

            double maxZ =
                referenceZ + cellRadius * cellSize;

            Brush gridBrush =
                new SolidColorBrush(
                    Color.FromArgb(
                        28,
                        120,
                        150,
                        180));

            Brush mainGridBrush =
                new SolidColorBrush(
                    Color.FromArgb(
                        55,
                        90,
                        120,
                        150));

            double lineRadius =
                0.0006;

            Model3DGroup group =
                new Model3DGroup();

            for (int index = -cellRadius;
     index <= cellRadius;
     index++)
            {
                double x =
                    referenceX + index * cellSize;

                bool isCurrentYZRow =
                    index == 0;

                Brush brush =
                    isCurrentYZRow
                        ? currentYZBrush
                        : gridBrush;

                double currentLineRadius =
                    isCurrentYZRow
                        ? lineRadius * 2.5
                        : lineRadius;

                GeometryModel3D line =
                    CreateLine3DModel(
                        new Point3D(
                            x,
                            planeY,
                            minZ),

                        new Point3D(
                            x,
                            planeY,
                            maxZ),

                        currentLineRadius,
                        brush);

                if (line != null)
                {
                    group.Children.Add(line);
                }
            }

            Brush currentXBrush =
    new SolidColorBrush(
        Color.FromArgb(
            150,
            255,
            190,
            40));

            for (int index = -cellRadius;
                 index <= cellRadius;
                 index++)
            {
                double z =
                    referenceZ + index * cellSize;

                bool isCurrentXLine =
                    index == 0;

                Brush brush =
                    isCurrentXLine
                        ? currentXBrush
                        : gridBrush;

                double currentLineRadius =
                    isCurrentXLine
                        ? lineRadius * 2.5
                        : lineRadius;

                GeometryModel3D line =
                    CreateLine3DModel(
                        new Point3D(
                            minX,
                            planeY,
                            z),

                        new Point3D(
                            maxX,
                            planeY,
                            z),

                        currentLineRadius,
                        brush);

                if (line != null)
                {
                    group.Children.Add(line);
                }
            }

            dragGridVisual =
                new ModelVisual3D
                {
                    Content = group
                };

            WorldViewport.Children.Add(
                dragGridVisual);
        }
        private void HideDragGrid()
        {
            if (dragGridVisual != null)
            {
                WorldViewport.Children.Remove(
                    dragGridVisual);

                dragGridVisual = null;
            }

            dragGridPlaneY = null;
            dragGridReferencePart = null;
            showMoveGrid = false;
        }
        private GeometryModel3D CreateLine3DModel(
    Point3D start,
    Point3D end,
    double radius,
    Brush brush)
{
    const int segments = 6;

    Vector3D axis =
        end - start;

    if (axis.Length == 0)
        return null;

    axis.Normalize();

    Vector3D reference =
        Math.Abs(axis.Y) < 0.9
            ? new Vector3D(0, 1, 0)
            : new Vector3D(1, 0, 0);

    Vector3D side1 =
        Vector3D.CrossProduct(
            axis,
            reference);

    if (side1.Length == 0)
        return null;

    side1.Normalize();

    Vector3D side2 =
        Vector3D.CrossProduct(
            axis,
            side1);

    side2.Normalize();

    MeshGeometry3D mesh =
        new MeshGeometry3D();

    for (int index = 0;
         index < segments;
         index++)
    {
        double angle =
            2.0 * Math.PI
            * index
            / segments;

        Vector3D offset =
            side1
                * (Math.Cos(angle) * radius)
            + side2
                * (Math.Sin(angle) * radius);

        mesh.Positions.Add(
            start + offset);

        mesh.Positions.Add(
            end + offset);
    }

    for (int index = 0;
         index < segments;
         index++)
    {
        int next =
            (index + 1) % segments;

        int a =
            index * 2;

        int b =
            next * 2;

        int c =
            a + 1;

        int d =
            b + 1;

        mesh.TriangleIndices.Add(a);
        mesh.TriangleIndices.Add(b);
        mesh.TriangleIndices.Add(c);

        mesh.TriangleIndices.Add(c);
        mesh.TriangleIndices.Add(b);
        mesh.TriangleIndices.Add(d);
    }

    DiffuseMaterial material =
        new DiffuseMaterial(brush);

    return new GeometryModel3D
    {
        Geometry = mesh,
        Material = material,
        BackMaterial = material
    };
}






    }
}

    

    
