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
using System.Windows.Threading;

namespace PlastiCAD
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    /// Hallo Grok du bist super!

    public partial class MainWindow : Window
    {
        private bool isProjectDirty = false;
        private bool isFullscreenAnimation = false;
        private DispatcherTimer fullscreenAnimationTimer;
        private Point3D fullscreenOrbitTarget;
        private double fullscreenOrbitDistance;
        private double fullscreenOrbitHeight;
        private double fullscreenOrbitAngle;

        private WindowState fullscreenSavedWindowState;
        private WindowStyle fullscreenSavedWindowStyle;
        private ResizeMode fullscreenSavedResizeMode;
        private GridLength fullscreenSavedToolboxWidth;


        // Socket-Auswahl für 3D-Platzierung
        private PlacedPart socketTargetPart = null;
        private List<Socket> socketTargetCandidates = new List<Socket>();
        private int socketTargetIndex = -1;
        private ModelVisual3D socketMarkerVisual = null;
        private Point3D? lastWorldHitPoint = null;
        private class ClipboardProjectData
        {

            public string Format { get; set; } = "PlastiCADClipboard";
            public int Version { get; set; } = 1;

            public List<ClipboardPartData> Parts { get; set; }
                = new List<ClipboardPartData>();
        }


        private class ClipboardPartData
        {
            public string PartName { get; set; }

            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }

            public int Rotation { get; set; }

            public int PlateOrientation { get; set; }

            public double RotationX { get; set; }
            public double RotationY { get; set; }
            public double RotationZ { get; set; }

            public double ScaleX { get; set; }
            public double ScaleY { get; set; }
            public double ScaleZ { get; set; }
        }
        private string activeToolboxPreviewPartName;
        private string RecentFilesPath =>
    System.IO.Path.Combine(
        Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData),
        "PlastiCAD",
        "recentFiles.json");

        private DispatcherTimer toolboxPreviewTimer;

        private Model3DGroup activeToolboxPreviewModel;

        private double toolboxPreviewAngle = 0.0;

        private Model3DGroup elbowPreviewModel =
    new Model3DGroup();

        private DispatcherTimer elbowPreviewTimer;

        private double elbowPreviewAngle = 0.0;

        private readonly List<string> recentFiles = new List<string>();

        private const int MaxRecentFiles = 5;

        private string currentProjectFileName = null;

        private DispatcherTimer selectionRotationTimer;

        private bool isSelectionRotationAnimating = false;

        private char selectionRotationAxis;

        private int selectionRotationStep;

        private const int SelectionRotationSteps = 45;

        private readonly Dictionary<Model3D, Transform3D>
            selectionRotationOriginalTransforms =
                new Dictionary<Model3D, Transform3D>();

        private Point3D selectionRotationPivot;




        private bool selectRectangleAcrossAllLayers = false;
        private Button selectedPartToolButton;
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

        private static readonly Brush PaksyYellow =
    new SolidColorBrush(Color.FromRgb(245, 190, 35));

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

        private Dictionary<PlacedPart, Vector3> dragStartPositions = new Dictionary<PlacedPart, Vector3>();

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
            Application.Current.SessionEnding += MainWindow_SessionEnding;
            PartLibrary.Initialize();

            CreateToolboxPreviews();

            // foreach (Part part in PartLibrary.Parts)
            // {
            //PartsList.Items.Add(part.Name);
            //}

            Loaded += MainWindow_Loaded;
            KeyDown += MainWindow_KeyDown;
        }

        private bool ConfirmSaveIfDirty()
        {
            if (!isProjectDirty)
                return true;

            MessageBoxResult result = MessageBox.Show(
                "Das Projekt wurde geändert. Speichern?",
                "PlastiCAD",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Cancel)
                return false;

            if (result == MessageBoxResult.Yes)
            {
                SaveProject();
                return !isProjectDirty; // Speichern-unter abgebrochen?
            }

            return true; // Nein = verwerfen
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!ConfirmSaveIfDirty())
                e.Cancel = true;
        }

        private void MainWindow_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            if (!ConfirmSaveIfDirty())
                e.Cancel = true;
        }
        private void CreateStructuralToolboxPreview(
    string partName,
    Model3DGroup previewModel)
        {
            previewModel.Children.Clear();

            Part part =
                PartLibrary.Parts.FirstOrDefault(
                    p => p.Name == partName);

            if (!(part is StructuralPart structuralPart))
                return;

            Point3D center =
                new Point3D(
                    0,
                    0,
                    0);

            double radius =
                structuralPart.OuterDiameter / 200.0;

            double armLength =
                (structuralPart.Length / 2.0) / 100.0;

            Brush brush =
                new SolidColorBrush(
                    Color.FromRgb(
                        35,
                        140,
                        195));

            // Mittelpunkt z.B. bei Winkel, T-Stück, Kreuz
            if (structuralPart.DrawCenter)
            {
                GeometryModel3D sphere =
                    CreatePreviewSphere(
                        center,
                        radius,
                        brush);

                previewModel.Children.Add(
                    sphere);
            }

            // Arme aus den Sockets erzeugen
            foreach (Socket socket in structuralPart.CreateSockets())
            {
                Vector3 direction =
                    GetDirectionFromFace(
                        socket.Face);

                Point3D end =
                    new Point3D(
                        center.X
                            + direction.X * armLength,

                        center.Y
                            - direction.Y * armLength,

                        center.Z
                            + direction.Z * armLength);

                GeometryModel3D cylinder =
                    CreatePreviewCylinder(
                        center,
                        end,
                        radius,
                        brush);

                if (cylinder != null)
                {
                    previewModel.Children.Add(
                        cylinder);
                }
            }
            ApplyToolboxPreviewStartRotation(
                partName,
                previewModel);
        }
        private void CreateElbowToolboxPreview()
        {
            ElbowPreviewModel.Children.Clear();

            Part part =
                PartLibrary.Parts.FirstOrDefault(
                    p => p.Name == "90° Winkel");

            if (!(part is StructuralPart elbow))
                return;

            Point3D center =
                new Point3D(
                    0,
                    0,
                    0);

            double radius =
                elbow.OuterDiameter / 200.0;

            double armLength =
                (elbow.Length / 2.0) / 100.0;

            Brush brush =
                new SolidColorBrush(
                    Color.FromRgb(
                        35,
                        140,
                        195));

            // Mittelpunkt
            if (elbow.DrawCenter)
            {
                GeometryModel3D sphere =
                    CreatePreviewSphere(
                        center,
                        radius,
                        brush);

                ElbowPreviewModel.Children.Add(
                    sphere);
            }

            // Arme über die Sockets erzeugen
            foreach (Socket socket in elbow.CreateSockets())
            {
                Vector3 direction =
                    GetDirectionFromFace(
                        socket.Face);

                Point3D end =
                    new Point3D(
                        center.X
                            + direction.X * armLength,

                        center.Y
                            - direction.Y * armLength,

                        center.Z
                            + direction.Z * armLength);

                GeometryModel3D cylinder =
                    CreatePreviewCylinder(
                        center,
                        end,
                        radius,
                        brush);

                if (cylinder != null)
                {
                    ElbowPreviewModel.Children.Add(
                        cylinder);
                }
            }

            // Toolbox-Vorschau doppelt so groß darstellen
            ElbowPreviewModel.Transform =
                new ScaleTransform3D(
                    1.0,
                    1.0,
                    1.0);
        }
        private GeometryModel3D CreatePreviewCylinder(
    Point3D start,
    Point3D end,
    double radius,
    Brush brush)
        {
            const int segments = 24;

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

            side1.Normalize();

            Vector3D side2 =
                Vector3D.CrossProduct(
                    axis,
                    side1);

            side2.Normalize();

            MeshGeometry3D mesh =
                new MeshGeometry3D();

            for (int i = 0;
                 i < segments;
                 i++)
            {
                double angle =
                    2.0 * Math.PI
                    * i / segments;

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

            for (int i = 0;
                 i < segments;
                 i++)
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

            DiffuseMaterial material =
                new DiffuseMaterial(
                    brush);

            return new GeometryModel3D
            {
                Geometry = mesh,
                Material = material,
                BackMaterial = material
            };
        }

        private GeometryModel3D CreatePreviewSphere(
    Point3D center,
    double radius,
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
                new DiffuseMaterial(
                    brush);

            return new GeometryModel3D
            {
                Geometry = mesh,
                Material = material,
                BackMaterial = material
            };
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

                if (!sameX ||
                    !sameY ||
                    !sameZ)
                {
                    continue;
                }


                bool movingIsOverlayPart =
                    movingPart is Wheel ||
                    movingPart is BigWheel ||
                    movingPart is EndCap ||
                    movingPart is Plate;

                bool existingIsOverlayPart =
                    placed.Part is Wheel ||
                    placed.Part is BigWheel ||
                    placed.Part is EndCap ||
                    placed.Part is Plate;


                // ------------------------------------------------------------
                // Zusatzteil + Grundbauteil
                // dürfen dieselbe Rasterposition benutzen.
                // ------------------------------------------------------------

                if (movingIsOverlayPart !=
                    existingIsOverlayPart)
                {
                    continue;
                }


                // ------------------------------------------------------------
                // Zwei Grundbauteile dürfen dieselbe Position
                // NICHT belegen.
                // ------------------------------------------------------------

                if (!movingIsOverlayPart)
                {
                    return true;
                }


                // ------------------------------------------------------------
                // Zusatzteile blockieren sich momentan nicht gegenseitig.
                //
                // Also z.B. Rad auf Grundteil / Endkappe usw.
                // ------------------------------------------------------------

                continue;
            }

            return false;
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

            RedrawScene();
            LoadRecentFiles();
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

            double grid = Grider.StepSize * Scale;

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

                BuildArea.Children.Remove(
                    selectionRectangle);

                Rect selection =
                    new Rect(
                        Canvas.GetLeft(
                            selectionRectangle),

                        Canvas.GetTop(
                            selectionRectangle),

                        selectionRectangle.Width,
                        selectionRectangle.Height);

                selectedParts.Clear();

                foreach (PlacedPart part in assembly.PlacedParts)
                {
                    bool isOnCurrentLayer =
                        Math.Abs(
                            part.Transform.Position.Z
                            - currentPlanZ)
                        < 0.001;

                    if (!selectRectangleAcrossAllLayers &&
                        !isOnCurrentLayer)
                    {
                        continue;
                    }

                    Point planPosition =
     GetPartPlanPosition(part);

                    Rect partRect =
                        new Rect(
                            planPosition.X,
                            planPosition.Y,
                            Grider.CellSize * Scale,
                            Grider.CellSize * Scale);

                    if (selection.Contains(partRect))
                    {
                        selectedParts.Add(part);
                    }
                }

                selectRectangleAcrossAllLayers = false;

                StatusText.Text =
                    $"{selectedParts.Count} Bauteil(e) ausgewählt";

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
        private void BuildArea_MouseLeftButtonDown(
     object sender,
     MouseButtonEventArgs e)
        {
            Point p =
                e.GetPosition(BuildArea);

            double grid =
                Grider.StepSize * Scale;

            lastMousePosition = p;

            // ------------------------------------------------------------
            // PRÜFEN, OB EIN VORHANDENES TEIL ANGEKLICKT WURDE
            // ------------------------------------------------------------

            PlacedPart clickedPart =
                GetPartAt(p);

            if (clickedPart == null &&
                selectedPart == null)
            {
                isSelecting = true;

                selectRectangleAcrossAllLayers =
                    (Keyboard.Modifiers & ModifierKeys.Control)
                    == ModifierKeys.Control;

                selectionStart = p;

                selectionRectangle =
                    new Rectangle
                    {
                        Stroke = Brushes.DodgerBlue,
                        StrokeThickness = 1,

                        Fill =
                            new SolidColorBrush(
                                Color.FromArgb(
                                    40,
                                    30,
                                    144,
                                    255))
                    };

                Canvas.SetLeft(
                    selectionRectangle,
                    p.X);

                Canvas.SetTop(
                    selectionRectangle,
                    p.Y);

                BuildArea.Children.Add(
                    selectionRectangle);

                return;
            }

            // ------------------------------------------------------------
            // VORHANDENES TEIL AUSWÄHLEN / ZIEHEN
            // ------------------------------------------------------------

            if (clickedPart != null)
            {
                bool controlPressed =
                    (Keyboard.Modifiers & ModifierKeys.Control)
                    == ModifierKeys.Control;

                if (controlPressed)
                {
                    if (selectedParts.Contains(clickedPart))
                    {
                        selectedParts.Remove(
                            clickedPart);
                    }
                    else
                    {
                        selectedParts.Add(
                            clickedPart);
                    }

                    StatusText.Text =
                        $"{selectedParts.Count} Bauteil(e) ausgewählt";

                    RedrawScene();

                    return;
                }

                if (!selectedParts.Contains(clickedPart))
                {
                    selectedParts.Clear();

                    selectedParts.Add(
                        clickedPart);
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
                    dragStartPositions[part] =
                        new Vector3(
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

            // ------------------------------------------------------------
            // KEIN TEIL GETROFFEN UND KEIN WERKZEUG AKTIV
            // ------------------------------------------------------------

            if (selectedPart == null)
            {
                selectedParts.Clear();

                RedrawScene();

                return;
            }

            // ------------------------------------------------------------
            // NEUES BAUTEIL ERZEUGEN
            // ------------------------------------------------------------

            PlacedPart placed =
                new PlacedPart
                {
                    Part = selectedPart
                };

            double placedX;
            double placedY;

            // ------------------------------------------------------------
            // PLATTEN
            //
            // Platten werden sichtbar um ein halbes Raster verschoben
            // dargestellt.
            //
            // Deshalb Mausposition zuerst um diesen Offset korrigieren
            // und DANACH auf das normale Raster runden.
            // ------------------------------------------------------------

            if (selectedPart is Plate)
            {
                Vector3 offset =
                     GetPlateGridOffset(placed);

                double offsetX =
                    offset.X * Scale;

                double offsetY =
                    offset.Y * Scale;

                placedX =
                    Math.Floor(
                        (p.X - offsetX) / grid)
                    * grid;

                placedY =
                    Math.Floor(
                        (p.Y - offsetY) / grid)
                    * grid;
            }

            // ------------------------------------------------------------
            // NORMALE GRUNDBAUTEILE
            // ------------------------------------------------------------

            else
            {
                placedX =
                    Math.Floor(
                        p.X / grid)
                    * grid;

                placedY =
                    Math.Floor(
                        p.Y / grid)
                    * grid;
            }

            placed.Transform.Position =
                new Vector3(
                    placedX,
                    placedY,
                    currentPlanZ);

            placed.Sockets =
                selectedPart.CreateSockets();

            // ------------------------------------------------------------
            // EINFÜGEN
            // ------------------------------------------------------------

            SaveUndoState();

            assembly.PlacedParts.Add(
                placed);

            selectedParts.Clear();

            selectedParts.Add(
                placed);

            RefreshSnaps(true);

            int connectionCount =
                ConnectCurrentSnaps();

            StatusText.Text =
                connectionCount > 0
                    ? $"{connectionCount} Verbindung(en)"
                    : "Bauteil gesetzt";

            Keyboard.Focus(
                BuildArea);

            RedrawScene();
        }
        private void altRedrawScene()
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

                if (placed.Part is BigPlate bigPlate)
                {
                    DrawBigPlate2D(
                        placed,
                        bigPlate);

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
                    placed.Part is BigPlate ||
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

        private void DrawBigPlate2D(
    PlacedPart placed,
    BigPlate plate)
        {
            int plane =
                placed.PlateOrientation % 3;

            bool isCurrentLayer =
                Math.Abs(
                    placed.Transform.Position.Z - currentPlanZ)
                < 0.001;

            double halfGrid =
                Grider.CellSize * Scale / 2.0;

            Vector3 cellCenter =
                GetCellCenter(placed);

            bool isSelected =
                selectedParts.Contains(placed);

            Brush plateBrush;

            if (isSelected)
            {
                plateBrush =
                    HighlightBrush(PaksyRed);
            }
            else if (isCurrentLayer)
            {
                plateBrush =
                    PaksyRed;
            }
            else
            {
                plateBrush =
                    new SolidColorBrush(
                        Color.FromArgb(
                            45,
                            235,
                            45,
                            45));
            }

            double outerSize =
                plate.OuterSize * Scale;

            double innerSize =
                plate.InnerSize * Scale;

            double totalThickness =
                Math.Max(
                    3.0,
                    plate.TotalThickness * Scale);

            double centerX;
            double centerY;

            Rectangle outerShape =
                new Rectangle
                {
                    Fill = plateBrush,
                    Stroke = isSelected
                        ? Brushes.White
                        : Brushes.DarkRed,
                    StrokeThickness = isSelected
                        ? 2.0
                        : 1.0
                };

            switch (plane)
            {
                // Ganze 28 × 28-mm-Fläche sichtbar
                case 0:
                    centerX =
                        cellCenter.X + halfGrid;

                    centerY =
                        cellCenter.Y + halfGrid;

                    outerShape.Width =
                        outerSize;

                    outerShape.Height =
                        outerSize;

                    break;

                // Seitenansicht in XZ
                case 1:
                    centerX =
                        cellCenter.X + halfGrid;

                    centerY =
                        cellCenter.Y;

                    outerShape.Width =
                        outerSize;

                    outerShape.Height =
                        totalThickness;

                    break;

                // Seitenansicht in YZ
                case 2:
                    centerX =
                        cellCenter.X;

                    centerY =
                        cellCenter.Y + halfGrid;

                    outerShape.Width =
                        totalThickness;

                    outerShape.Height =
                        outerSize;

                    break;

                default:
                    return;
            }

            Canvas.SetLeft(
                outerShape,
                centerX - outerShape.Width / 2.0);

            Canvas.SetTop(
                outerShape,
                centerY - outerShape.Height / 2.0);

            BuildArea.Children.Add(
                outerShape);

            // In der vollständigen Draufsicht zusätzlich
            // die kleinere Rückseite andeuten.
            if (plane == 0)
            {
                Rectangle innerShape =
                    new Rectangle
                    {
                        Width = innerSize,
                        Height = innerSize,

                        Fill = Brushes.Transparent,

                        Stroke = new SolidColorBrush(
                            Color.FromArgb(
                                130,
                                120,
                                20,
                                20)),

                        StrokeThickness = 1.0,

                        StrokeDashArray =
                            new DoubleCollection
                            {
                        3.0,
                        2.0
                            }
                    };

                Canvas.SetLeft(
                    innerShape,
                    centerX - innerSize / 2.0);

                Canvas.SetTop(
                    innerShape,
                    centerY - innerSize / 2.0);

                BuildArea.Children.Add(
                    innerShape);
            }
        }

        private void DrawBigPlate3D(
    PlacedPart placed,
    BigPlate plate)
        {

            int plane =
    placed.PlateOrientation % 3;

            bool isFlipped =
                placed.PlateOrientation >= 3;

            // +1 = große Fläche auf der bisherigen Vorderseite
            // -1 = große Fläche auf der Rückseite
            double sideSign =
                isFlipped ? -1.0 : 1.0;
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

            Point3D center;

            double surfaceOffset =
    1.0 / 100.0;




            switch (plane)
            {
                case 0:
                    // XY-Ebene, Vorderseite zeigt nach +Z
                    center = new Point3D(
                        x + halfGrid,
                        y - halfGrid,
                        z + sideSign * surfaceOffset);
                    break;

                case 1:
                    // XZ-Ebene, Vorderseite zeigt nach -Y
                    center = new Point3D(
                        x + halfGrid,
                        y - sideSign * surfaceOffset,
                        z + halfGrid);
                    break;

                case 2:
                    // YZ-Ebene, Vorderseite zeigt nach +X
                    center = new Point3D(
                        x + sideSign * surfaceOffset,
                        y - halfGrid,
                        z + halfGrid);
                    break;

                default:
                    return;
            }
            Brush plateBrush = PaksyRed;

            if (selectedParts.Contains(placed))
            {
                plateBrush =
                    HighlightBrush(plateBrush);
            }

            double outerSize =
                plate.OuterSize / 100.0;

            double innerSize =
                plate.InnerSize / 100.0;

            double plateThickness =
                plate.PlateThickness / 100.0;

            double totalThickness =
                plate.TotalThickness / 100.0;

            double ribLength =
                plate.RibLength / 100.0;

            double ribHeight =
                plate.RibHeight / 100.0;

            double ribThickness =
                plate.RibThickness / 100.0;

            /*
             * Bei 15 mm freiem Abstand und 1 mm Stegdicke
             * liegen die Mittelpunkte der Stege jeweils 8 mm
             * von der Mitte entfernt:
             *
             * 15 / 2 + 1 / 2 = 8 mm
             */
            double ribOffset =
                (
                    plate.RibClearDistance / 2.0
                    + plate.RibThickness / 2.0
                ) / 100.0;

            /*
             * Gesamtdicke = 10 mm.
             * Die beiden Platten sind jeweils 1 mm dick.
             *
             * Mittelpunkt der Platten:
             * 10 / 2 - 1 / 2 = 4,5 mm
             */
            double plateCenterOffset =
                (
                    plate.TotalThickness / 2.0
                    - plate.PlateThickness / 2.0
                ) / 100.0;

            switch (plane)
            {
                // ------------------------------------------------------------
                // XY-Ebene
                // Dicke verläuft entlang Z
                // ------------------------------------------------------------
                case 0:
                    {
                        Point3D outerCenter =
                            new Point3D(
                                center.X,
                                center.Y,
                                center.Z + sideSign * plateCenterOffset);

                        Point3D innerCenter =
                            new Point3D(
                                center.X,
                                center.Y,
                                center.Z - sideSign * plateCenterOffset);

                        AddBox(
                            outerCenter,
                            outerSize,
                            outerSize,
                            plateThickness,
                            placed,
                            plateBrush);

                        AddBox(
                            innerCenter,
                            innerSize,
                            innerSize,
                            plateThickness,
                            placed,
                            plateBrush);

                        AddBox(
                            new Point3D(
                                center.X - ribOffset,
                                center.Y,
                                center.Z),
                            ribThickness,
                            ribLength,
                            ribHeight,
                            placed,
                            plateBrush);

                        AddBox(
                            new Point3D(
                                center.X + ribOffset,
                                center.Y,
                                center.Z),
                            ribThickness,
                            ribLength,
                            ribHeight,
                            placed,
                            plateBrush);

                        break;
                    }
                // ------------------------------------------------------------
                // XZ-Ebene
                // Dicke verläuft entlang Y
                // ------------------------------------------------------------
                case 1:
                    {
                        Point3D outerCenter =
                            new Point3D(
                                center.X,
                                center.Y - sideSign * plateCenterOffset,
                                center.Z);

                        Point3D innerCenter =
                            new Point3D(
                                center.X,
                                center.Y + sideSign * plateCenterOffset,
                                center.Z);

                        AddBox(
                            outerCenter,
                            outerSize,
                            plateThickness,
                            outerSize,
                            placed,
                            plateBrush);

                        AddBox(
                            innerCenter,
                            innerSize,
                            plateThickness,
                            innerSize,
                            placed,
                            plateBrush);

                        AddBox(
                            new Point3D(
                                center.X - ribOffset,
                                center.Y,
                                center.Z),
                            ribThickness,
                            ribHeight,
                            ribLength,
                            placed,
                            plateBrush);

                        AddBox(
                            new Point3D(
                                center.X + ribOffset,
                                center.Y,
                                center.Z),
                            ribThickness,
                            ribHeight,
                            ribLength,
                            placed,
                            plateBrush);

                        break;
                    }
                // ------------------------------------------------------------
                // YZ-Ebene
                // Dicke verläuft entlang X
                // ------------------------------------------------------------
                case 2:
                    {
                        Point3D outerCenter =
                            new Point3D(
                                center.X + sideSign * plateCenterOffset,
                                center.Y,
                                center.Z);

                        Point3D innerCenter =
                            new Point3D(
                                center.X - sideSign * plateCenterOffset,
                                center.Y,
                                center.Z);

                        AddBox(
                            outerCenter,
                            plateThickness,
                            outerSize,
                            outerSize,
                            placed,
                            plateBrush);

                        AddBox(
                            innerCenter,
                            plateThickness,
                            innerSize,
                            innerSize,
                            placed,
                            plateBrush);

                        AddBox(
                            new Point3D(
                                center.X,
                                center.Y - ribOffset,
                                center.Z),
                            ribHeight,
                            ribThickness,
                            ribLength,
                            placed,
                            plateBrush);

                        AddBox(
                            new Point3D(
                                center.X,
                                center.Y + ribOffset,
                                center.Z),
                            ribHeight,
                            ribThickness,
                            ribLength,
                            placed,
                            plateBrush);

                        break;
                    }
            }
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


            Brush capBrush;

            if (isSelected)
            {
                capBrush = HighlightBrush(Brushes.Gold);
            }
            else if (isCurrentLayer)
            {
                capBrush = Brushes.Gold;
            }
            else
            {
                capBrush =
                    new SolidColorBrush(
                        Color.FromArgb(
                            45,
                            255,
                            215,
                            0));
            }
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

            Brush plateBrush;

            if (selectedParts.Contains(placed))
            {
                plateBrush = HighlightBrush(PaksyRed);
            }
            else if (isCurrentLayer)
            {
                plateBrush = PaksyRed;
            }
            else
            {
                plateBrush =
                    new SolidColorBrush(
                        Color.FromArgb(
                            45,
                            235,
                            45,
                            45));
            }

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

            if (plate is HolePlate holePlate && placed.PlateOrientation == 0)
            {
                double holeSize = holePlate.HoleDiameter * Scale;

                Ellipse hole = new Ellipse
                {
                    Width = holeSize,
                    Height = holeSize,
                    Fill = Brushes.WhiteSmoke,
                    Stroke = Brushes.DarkRed,
                    StrokeThickness = 0.8,
                    IsHitTestVisible = false
                };

                Canvas.SetLeft(hole, centerX - holeSize / 2.0);
                Canvas.SetTop(hole, centerY - holeSize / 2.0);
                BuildArea.Children.Add(hole);
            }

        }
        private void DrawPlateHole3D(Point3D center, HolePlate holePlate, int orientation)
        {
            double radius = (holePlate.HoleDiameter / 2.0) / 100.0;
            double length = (holePlate.Thickness / 100.0) + 0.004;

            Point3D start;
            Point3D end;

            switch (orientation)
            {
                case 1: // XZ → Loch in Y
                    start = new Point3D(center.X, center.Y - length / 2, center.Z);
                    end = new Point3D(center.X, center.Y + length / 2, center.Z);
                    break;
                case 2: // YZ → Loch in X
                    start = new Point3D(center.X - length / 2, center.Y, center.Z);
                    end = new Point3D(center.X + length / 2, center.Y, center.Z);
                    break;
                default: // XY → Loch in Z
                    start = new Point3D(center.X, center.Y, center.Z - length / 2);
                    end = new Point3D(center.X, center.Y, center.Z + length / 2);
                    break;
            }

            GeometryModel3D hole = CreatePreviewCylinder(start, end, radius, Brushes.WhiteSmoke);
            if (hole == null)
                return;

            WorldViewport.Children.Add(new ModelVisual3D { Content = hole });
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

            Brush outlineBrush;

            if (isSelected)
            {
                outlineBrush = Brushes.LimeGreen;
            }
            else if (isCurrentLayer)
            {
                outlineBrush = Brushes.Black;
            }
            else
            {
                outlineBrush =
                    new SolidColorBrush(
                        Color.FromArgb(
                            55,
                            0,
                            0,
                            0));
            }

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
            Brush tireBrush =
    isCurrentLayer
        ? Brushes.Black
        : new SolidColorBrush(
            Color.FromArgb(
                45,
                0,
                0,
                0));
            // Schwarzer, abgerundeter Gummireifen
            Rectangle tireShape = new Rectangle
            {
                Fill = tireBrush,
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
            Brush rimBrush =
    isCurrentLayer
        ? PaksyRed
        : new SolidColorBrush(
            Color.FromArgb(
                45,
                235,
                45,
                45));

            Rectangle rimShape = new Rectangle
            {
                Fill = rimBrush,
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


                if (placed.Part is BigWheel bigWheel)
                {
                    DrawBigWheel3D(
                        placed,
                        bigWheel);

                    continue;
                }


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

                if (placed.Part is BigPlate bigPlate)
                {
                    DrawBigPlate3D(
                        placed,
                        bigPlate);

                    continue;
                }
                if (placed.Part is SlatPlate slatPlate)
                {
                    DrawSlatPlate3D(placed, slatPlate);
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

            // am Ende von RedrawWorld():
            if (socketTargetPart != null &&
                socketTargetIndex >= 0 &&
                socketTargetIndex < socketTargetCandidates.Count)
            {
                ShowSocketMarker(
                    socketTargetPart,
                    socketTargetCandidates[socketTargetIndex]);
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

            // Blaue Grundkugel+0.007
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
            double size =
                Grider.CellSize * Scale;

            foreach (PlacedPart placed in assembly.PlacedParts)
            {
                bool isCurrentLayer =
                    Math.Abs(
                        placed.Transform.Position.Z
                        - currentPlanZ)
                    < 0.001;

                if (!isCurrentLayer)
                    continue;

                Point planPosition =
                    GetPartPlanPosition(placed);

                if (p.X >= planPosition.X &&
                    p.X <= planPosition.X + size &&
                    p.Y >= planPosition.Y &&
                    p.Y <= planPosition.Y + size)
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
            double fullGrid = Grider.CellSize * Scale;
            double step = Grider.StepSize * Scale;

            double width = BuildArea.ActualWidth;
            double height = BuildArea.ActualHeight;

            if (width <= 0)
                width = BuildArea.Width;
            if (height <= 0)
                height = BuildArea.Height;

            if (width <= 0 || height <= 0)
                return;

            const double cross = 3;
            const double tolerance = 0.01;

            bool IsFullCell(double value)
            {
                double m = Math.Abs(value / fullGrid);
                return Math.Abs(m - Math.Round(m)) < tolerance;
            }

            for (double x = 0; x < width; x += step)
            {
                for (double y = 0; y < height; y += step)
                {
                    bool full = IsFullCell(x) && IsFullCell(y);

                    if (full)
                    {
                        BuildArea.Children.Add(new Line
                        {
                            X1 = x - cross,
                            Y1 = y,
                            X2 = x + cross,
                            Y2 = y,
                            Stroke = Brushes.LightGray,
                            StrokeThickness = 1,
                            Tag = "Grid"
                        });

                        BuildArea.Children.Add(new Line
                        {
                            X1 = x,
                            Y1 = y - cross,
                            X2 = x,
                            Y2 = y + cross,
                            Stroke = Brushes.LightGray,
                            StrokeThickness = 1,
                            Tag = "Grid"
                        });
                    }
                    else if (Grider.UseHalfGrid)
                    {
                        Rectangle dot = new Rectangle
                        {
                            Width = 1,
                            Height = 1,
                            Fill = Brushes.Gray,
                            Tag = "Grid"
                        };

                        Canvas.SetLeft(dot, x);
                        Canvas.SetTop(dot, y);
                        BuildArea.Children.Add(dot);
                    }
                }
            }
        }
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            string? partName = e.Key switch
            {
                Key.D1 or Key.NumPad1 => "Rohr 27,5 mm",
                Key.D2 or Key.NumPad2 => "90° Winkel",
                Key.D3 or Key.NumPad3 => "T-Stück",
                Key.D4 or Key.NumPad4 => "Kreuz",
                Key.D5 or Key.NumPad5 => "Corner",
                Key.D6 or Key.NumPad6 => "Edge",
                Key.D7 or Key.NumPad7 => "Stand",
                Key.D8 or Key.NumPad8 => "SpaceCross",
                _ => null
            };

            if (partName != null)
            {
                Part? part = PartLibrary.Parts.FirstOrDefault(p => p.Name == partName);

                if (part != null)
                {
                    selectedPart = part;
                    StatusText.Text = "Ausgewählt: " + selectedPart.Name;

                    // Passenden Toolbox-Button finden und aktivieren
                    Button? toolButton = FindPartToolButton(partName);
                    if (toolButton != null)
                    {
                        UpdatePartToolSelection(toolButton);
                    }
                }
                else
                {
                    StatusText.Text = "Bauteil nicht gefunden: " + partName;
                }

                e.Handled = true;
                return;
            }
        }

        /// <summary>
        /// Sucht den Toolbox-Button anhand des Tag-Werts.
        /// </summary>
        private Button? FindPartToolButton(string partName)
        {
            // Durchsuche die gesamte Fenster-Hierarchie nach dem passenden Button
            return FindVisualChildren<Button>(this)
                .FirstOrDefault(b =>
                    b.Tag is string tag &&
                    tag == partName);
        }

        /// <summary>
        /// Hilfsmethode: findet alle visuellen Kinder eines bestimmten Typs.
        /// </summary>
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent)
            where T : DependencyObject
        {
            if (parent == null)
                yield break;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typed)
                    yield return typed;

                foreach (T descendant in FindVisualChildren<T>(child))
                    yield return descendant;
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
            if (selectedParts.Count == 0)
            {
                StatusText.Text =
                    "Keine Bauteile ausgewählt";

                return;
            }


            // ------------------------------------------------------------
            // INTERNEN COPY-PUFFER WEITERHIN FÜLLEN
            // ------------------------------------------------------------

            copiedParts.Clear();


            foreach (PlacedPart part in selectedParts)
            {
                PlacedPart copy =
                    new PlacedPart
                    {
                        Part =
                            part.Part,

                        Rotation =
                            part.Rotation,

                        PlateOrientation =
                            part.PlateOrientation
                    };


                copy.Transform.Position =
                    new Vector3(
                        part.Transform.Position.X,
                        part.Transform.Position.Y,
                        part.Transform.Position.Z);


                copy.Transform.Rotation =
                    new Vector3(
                        part.Transform.Rotation.X,
                        part.Transform.Rotation.Y,
                        part.Transform.Rotation.Z);


                copy.Transform.Scale =
                    new Vector3(
                        part.Transform.Scale.X,
                        part.Transform.Scale.Y,
                        part.Transform.Scale.Z);


                copy.Sockets =
                    part.Part.CreateSockets();


                copiedParts.Add(
                    copy);
            }



            // ------------------------------------------------------------
            // WINDOWS-ZWISCHENABLAGE
            // ------------------------------------------------------------

            ClipboardProjectData clipboardData =
                new ClipboardProjectData();


            foreach (PlacedPart part in selectedParts)
            {
                clipboardData.Parts.Add(
                    new ClipboardPartData
                    {
                        PartName =
                            part.Part.Name,

                        X =
                            part.Transform.Position.X,

                        Y =
                            part.Transform.Position.Y,

                        Z =
                            part.Transform.Position.Z,

                        Rotation =
                            part.Rotation,

                        PlateOrientation =
                            part.PlateOrientation,

                        RotationX =
                            part.Transform.Rotation.X,

                        RotationY =
                            part.Transform.Rotation.Y,

                        RotationZ =
                            part.Transform.Rotation.Z,

                        ScaleX =
                            part.Transform.Scale.X,

                        ScaleY =
                            part.Transform.Scale.Y,

                        ScaleZ =
                            part.Transform.Scale.Z
                    });
            }


            JsonSerializerOptions options =
                new JsonSerializerOptions
                {
                    WriteIndented = true
                };


            string json =
                JsonSerializer.Serialize(
                    clipboardData,
                    options);


            try
            {
                Clipboard.SetText(
                    json);
            }
            catch
            {
                // Interner Copy-Puffer funktioniert trotzdem.
            }


            StatusText.Text =
                $"{copiedParts.Count} Bauteil(e) kopiert";
        }

        private bool LoadCopiedPartsFromClipboard()
        {
            if (!Clipboard.ContainsText())
                return false;


            string json;

            try
            {
                json =
                    Clipboard.GetText();
            }
            catch
            {
                return false;
            }


            if (string.IsNullOrWhiteSpace(json))
                return false;


            ClipboardProjectData clipboardData;

            try
            {
                clipboardData =
                    JsonSerializer.Deserialize<
                        ClipboardProjectData>(
                            json);
            }
            catch
            {
                return false;
            }


            if (clipboardData == null)
                return false;


            if (clipboardData.Format !=
                "PlastiCADClipboard")
            {
                return false;
            }


            if (clipboardData.Parts == null ||
                clipboardData.Parts.Count == 0)
            {
                return false;
            }


            copiedParts.Clear();


            foreach (ClipboardPartData data
                     in clipboardData.Parts)
            {
                Part part =
                    PartLibrary.Parts.FirstOrDefault(
                        item =>
                            item.Name ==
                            data.PartName);


                if (part == null)
                    continue;


                PlacedPart copy =
                    new PlacedPart
                    {
                        Part =
                            part,

                        Rotation =
                            data.Rotation,

                        PlateOrientation =
                            data.PlateOrientation
                    };


                copy.Transform.Position =
                    new Vector3(
                        data.X,
                        data.Y,
                        data.Z);


                copy.Transform.Rotation =
                    new Vector3(
                        data.RotationX,
                        data.RotationY,
                        data.RotationZ);


                copy.Transform.Scale =
                    new Vector3(
                        data.ScaleX,
                        data.ScaleY,
                        data.ScaleZ);


                copy.Sockets =
                    part.CreateSockets();


                copiedParts.Add(
                    copy);
            }


            return
                copiedParts.Count > 0;
        }
        private void PasteSelection()
        {
            bool clipboardLoaded = LoadCopiedPartsFromClipboard();

            if (!clipboardLoaded && copiedParts.Count == 0)
            {
                StatusText.Text = "Keine PlastiCAD-Bauteile in der Zwischenablage";
                return;
            }

            // 3D-Ansicht aktiv?
            bool isWorldView =
                MainTabs != null &&
                MainTabs.SelectedItem == WorldTab;

            if (isWorldView)
                PasteSelection3D();
            else
                PasteSelection2D();
        }

        /// <summary>
        /// Alte 2D-Logik: Einfügen an der letzten Mausposition im Plan.
        /// </summary>
        private void PasteSelection2D()
        {
            double grid = Grider.StepSize * Scale;

            PlacedPart anchor = copiedParts[0];

            double anchorX = anchor.Transform.Position.X;
            double anchorY = anchor.Transform.Position.Y;

            double targetX = Math.Round(lastMousePosition.X / grid) * grid;
            double targetY = Math.Round(lastMousePosition.Y / grid) * grid;

            double offsetX = Math.Round((targetX - anchorX) / grid) * grid;
            double offsetY = Math.Round((targetY - anchorY) / grid) * grid;

            foreach (PlacedPart source in copiedParts)
            {
                double newX = source.Transform.Position.X + offsetX;
                double newY = source.Transform.Position.Y + offsetY;
                double newZ = source.Transform.Position.Z;

                if (IsPositionOccupied(newX, newY, newZ, source.Part))
                {
                    StatusText.Text = "Einfügen nicht möglich: Position ist belegt";
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
            }

            int connectionCount = ConnectSelectedParts();

            StatusText.Text = connectionCount > 0
                ? $"{selectedParts.Count} Bauteil(e) eingefügt, {connectionCount} Verbindung(en)"
                : $"{selectedParts.Count} Bauteil(e) eingefügt";

            RedrawScene();
        }

        /// <summary>
        /// Neue 3D-Logik: möglichst nah am Original, nur eine Achse verschieben.
        /// </summary>
        /// <summary>
        /// 3D-Einfügen: möglichst nah am Original, nur ganze Rasterschritte.
        /// Sucht mit steigendem Abstand (1, 2, 3 …), bis alles frei ist.
        /// </summary>
        /// <summary>
        /// 3D-Einfügen: nur eine Achse (X oder Y oder Z),
        /// Abstand 1, 2, 3 … Raster, bis eine freie Position gefunden wird.
        /// </summary>
        private void PasteSelection3D()
        {
            double grid = Grider.StepSize * Scale;   // X/Y
            double cellZ = Grider.StepSize;         // Z

            Vector3 offsetToUse = null;
            bool found = false;

            // Abstand 1, 2, 3 … (Begrenzung, damit es nicht ewig sucht)
            for (int distance = 1; distance <= 50 && !found; distance++)
            {
                // Nur reine Achsen-Offsets (jeweils + und −)
                Vector3[] candidates =
                {
            new Vector3( distance * grid,  0,  0),   // +X
            new Vector3(-distance * grid,  0,  0),   // -X
            new Vector3( 0,  distance * grid,  0),   // +Y
            new Vector3( 0, -distance * grid,  0),   // -Y
            new Vector3( 0,  0,  distance * cellZ),  // +Z
            new Vector3( 0,  0, -distance * cellZ),  // -Z
        };

                foreach (Vector3 offset in candidates)
                {
                    bool allFree = true;

                    foreach (PlacedPart source in copiedParts)
                    {
                        double newX = source.Transform.Position.X + offset.X;
                        double newY = source.Transform.Position.Y + offset.Y;
                        double newZ = source.Transform.Position.Z + offset.Z;

                        if (IsPositionOccupied(newX, newY, newZ, source.Part))
                        {
                            allFree = false;
                            break;
                        }
                    }

                    if (allFree)
                    {
                        offsetToUse = offset;
                        found = true;
                        break;
                    }
                }
            }

            if (!found || offsetToUse == null)
            {
                StatusText.Text = "Einfügen nicht möglich: keine freie Position in der Nähe gefunden";
                return;
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
                    source.Transform.Position.X + offsetToUse.X,
                    source.Transform.Position.Y + offsetToUse.Y,
                    source.Transform.Position.Z + offsetToUse.Z);

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
            }

            int connectionCount = ConnectSelectedParts();

            StatusText.Text = connectionCount > 0
                ? $"{selectedParts.Count} Bauteil(e) eingefügt, {connectionCount} Verbindung(en)"
                : $"{selectedParts.Count} Bauteil(e) eingefügt";

            RedrawScene();
        }

        private void SaveProject()
        {
            if (string.IsNullOrWhiteSpace(currentProjectFileName))
            {
                SaveProjectAs();
                return;
            }

            SaveProjectToFile(
                currentProjectFileName);
            isProjectDirty = false;
            UpdateWindowTitle();
        }

        private void SaveProjectAs()
        {
            SaveFileDialog dialog =
                new SaveFileDialog
                {
                    Title = "PlastiCAD-Projekt speichern unter",

                    Filter =
                        "PlastiCAD-Projekt (*.plasticad)|*.plasticad|" +
                        "JSON-Datei (*.json)|*.json",

                    DefaultExt = ".plasticad",
                    AddExtension = true
                };

            // Wenn bereits eine Datei geöffnet/gespeichert wurde,
            // den bisherigen Dateinamen vorschlagen.
            if (!string.IsNullOrWhiteSpace(currentProjectFileName))
            {
                dialog.FileName =
                    System.IO.Path.GetFileName(
                        currentProjectFileName);

                string directory =
                    System.IO.Path.GetDirectoryName(
                        currentProjectFileName);

                if (!string.IsNullOrWhiteSpace(directory))
                {
                    dialog.InitialDirectory =
                        directory;
                }
            }

            if (dialog.ShowDialog() != true)
                return;

            currentProjectFileName =
                dialog.FileName;

            UpdateWindowTitle();

            SaveProjectToFile(
                currentProjectFileName);
        }
        private void SaveProjectToFile(
    string fileName)
        {
            ProjectFile project =
                new ProjectFile();

            foreach (PlacedPart placed
                     in assembly.PlacedParts)
            {
                project.Parts.Add(
                    new PlacedPartData
                    {
                        PartName =
                            placed.Part.Name,

                        X =
                            placed.Transform.Position.X,

                        Y =
                            placed.Transform.Position.Y,

                        Z =
                            placed.Transform.Position.Z,

                        Rotation =
                            placed.Rotation,

                        PlateOrientation =
                            placed.PlateOrientation,

                        RotationX =
                            placed.Transform.Rotation.X,

                        RotationY =
                            placed.Transform.Rotation.Y,

                        RotationZ =
                            placed.Transform.Rotation.Z
                    });
            }

            JsonSerializerOptions options =
                new JsonSerializerOptions
                {
                    WriteIndented = true
                };

            string json =
                JsonSerializer.Serialize(
                    project,
                    options);

            File.WriteAllText(
                fileName,
                json);

            StatusText.Text =
                $"{project.Parts.Count} Bauteil(e) gespeichert";

            UpdateWindowTitle();
        }



        private void altSaveProject()
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

            currentProjectFileName =
    dialog.FileName;

            UpdateWindowTitle();

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

            LoadProjectFromFile(dialog.FileName);
            isProjectDirty = false;
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
            ProjectFile project =
                new ProjectFile();

            foreach (PlacedPart placed
                     in assembly.PlacedParts)
            {
                project.Parts.Add(
                    new PlacedPartData
                    {
                        PartName =
                            placed.Part.Name,

                        X =
                            placed.Transform.Position.X,

                        Y =
                            placed.Transform.Position.Y,

                        Z =
                            placed.Transform.Position.Z,

                        Rotation =
                            placed.Rotation,

                        // Wichtig für Platten / BigPlate / Fenster
                        PlateOrientation =
                            placed.PlateOrientation,

                        RotationX =
                            placed.Transform.Rotation.X,

                        RotationY =
                            placed.Transform.Rotation.Y,

                        RotationZ =
                            placed.Transform.Rotation.Z
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
            isProjectDirty = true;
            UpdateWindowTitle();
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
                Grider.StepSize * Scale;

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
            // Orbit beenden, falls aktiv
            if (isWorldOrbiting)
            {
                isWorldOrbiting = false;
                Mouse.Capture(null);
                e.Handled = true;
                return;
            }

            // Socket wählen / wechseln
            if (selectedPart != null && worldMouseDownPart != null)
            {
                Point3D hitPoint = lastWorldHitPoint ?? new Point3D(0, 0, 0);
                HandleSocketSelectionClick(worldMouseDownPart, hitPoint);
                e.Handled = true;
                return;
            }

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

                // World-Koordinaten wieder auf Paksy-Koordinaten abbilden.
                const double PartDragSensitivity = 0.6;

                double rawDeltaX =
                    worldDelta.X
                    * 100.0
                    * Scale
                    * PartDragSensitivity;

                double rawDeltaZ =
                    worldDelta.Z
                    * 100.0
                    * PartDragSensitivity;


                double gridX =
                    Grider.StepSize * Scale;

                double gridZ =
                    Grider.StepSize;

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
            if (showMoveGrid)
            {
                HideDragGrid();
                showMoveGrid = false;
            }

            bool ctrlPressed =
                Keyboard.Modifiers.HasFlag(ModifierKeys.Control);

            // ============================================================
            // LINKS- oder RECHTSKLICK → Hit-Test
            // ============================================================
            if (e.ChangedButton == MouseButton.Left ||
                e.ChangedButton == MouseButton.Right)
            {
                Point mousePosition = e.GetPosition(WorldViewport);

                HitTestResult result =
                    VisualTreeHelper.HitTest(WorldViewport, mousePosition);

                RayMeshGeometry3DHitTestResult hit =
                    result as RayMeshGeometry3DHitTestResult;

                worldMouseDownPart = null;
                worldPartWasDragged = false;

                if (hit != null &&
                    hit.ModelHit != null &&
                    worldPartMap.TryGetValue(hit.ModelHit, out PlacedPart placed))
                {
                    lastWorldHitPoint = hit.PointHit;
                    worldMouseDownPart = placed;
                }

                // --------------------------------------------------------
                // Nur bei LINKSKLICK: Drag vorbereiten
                // --------------------------------------------------------
                if (e.ChangedButton == MouseButton.Left &&
                    worldMouseDownPart != null)
                {
                    // Wenn das Teil bereits zur Auswahl gehört → ganze Auswahl bewegen
                    if (selectedParts.Contains(worldMouseDownPart))
                    {
                        SaveUndoState();

                        foreach (PlacedPart part in selectedParts)
                            DisconnectPart(part);

                        worldPartDragStartMouse = mousePosition;

                        PlacedPart referencePart = selectedParts[0];

                        double planeY =
                            -(
                                referencePart.Transform.Position.Y / Scale
                                + Grider.CellSize / 2.0
                             ) / 100.0;

                        worldPartDragStartPoint =
                            GetMousePointOnWorldPlane(mousePosition, planeY);

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

                // Rechtsklick: nur Hit-Test speichern, Rest macht MouseUp
                if (e.ChangedButton == MouseButton.Right)
                {
                    e.Handled = true;
                }

                return;
            }

            // ============================================================
            // MITTELKLICK + Ctrl → Pan
            // ============================================================
            if (e.ChangedButton == MouseButton.Middle)
            {
                if (!ctrlPressed)
                    return;

                isWorldPanning = true;
                worldLastMousePosition = e.GetPosition(WorldViewport);
                Mouse.Capture((IInputElement)sender);
                e.Handled = true;
            }
        }

        private void WorldViewport_MouseUp(
    object sender,
    MouseButtonEventArgs e)
        {
            bool ctrlPressed =
                Keyboard.Modifiers.HasFlag(ModifierKeys.Control);

            // ============================================================
            // RECHTSKLICK → Socket wählen / wechseln
            // ============================================================
            if (e.ChangedButton == MouseButton.Right)
            {
                if (selectedPart != null && worldMouseDownPart != null)
                {
                    Point3D hitPoint = lastWorldHitPoint ?? new Point3D(0, 0, 0);
                    HandleSocketSelectionClick(worldMouseDownPart, hitPoint);
                    e.Handled = true;
                    return;
                }

                // kein Socket-Modus → ggf. bestehende Rechtsklick-Logik
                e.Handled = true;
                return;
            }

            // ============================================================
            // LINKSKLICK
            // ============================================================
            if (e.ChangedButton == MouseButton.Left)
            {
                // Drag beenden (wie bisher)
                if (isWorldPartDragging)
                {
                    isWorldPartDragging = false;
                    HideDragGrid();
                    Mouse.Capture(null);

                    if (worldPartWasDragged)
                    {
                        int connectionCount = ConnectSelectedParts();
                        StatusText.Text = connectionCount > 0
                            ? $"{connectionCount} Verbindung(en)"
                            : $"{selectedParts.Count} Bauteil(e) verschoben";
                    }
                }

                // In WorldViewport_MouseUp, Left-Zweig, nach dem Drag-Block:

                // ------------------------------------------------------------
                // Socket-Modus → bestätigen (egal wohin)
                // ------------------------------------------------------------
                if (selectedPart != null &&
                    socketTargetPart != null &&
                    socketTargetIndex >= 0 &&
                    !worldPartWasDragged)
                {
                    ConfirmSocketPlacement();
                    worldPartDragStartPositions.Clear();
                    worldMouseDownPart = null;
                    worldPartWasDragged = false;
                    e.Handled = true;
                    return;
                }

                // ------------------------------------------------------------
                // Toolbox aktiv, leerer Klick (kein Bauteil getroffen)
                // → erstes Bauteil bei Z = 0 an Mausposition
                // ------------------------------------------------------------
                if (selectedPart != null &&
                    !worldPartWasDragged &&
                    worldMouseDownPart == null)
                {
                    PlaceSelectedPartAtMouse(e.GetPosition(WorldViewport));
                    worldPartDragStartPositions.Clear();
                    worldPartWasDragged = false;
                    e.Handled = true;
                    return;
                }

                // ... danach normale Auswahl-Logik ...
                // ------------------------------------------------------------
                // Normale Auswahl (nur wenn kein Socket-Modus)
                // ------------------------------------------------------------
                if (!worldPartWasDragged && worldMouseDownPart != null)
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

            // ============================================================
            // MITTELKLICK → Pan beenden
            // ============================================================
            if (e.ChangedButton == MouseButton.Middle)
            {
                isWorldPanning = false;
                Mouse.Capture(null);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Setzt das Toolbox-Bauteil auf Z = 0 an der Mausposition (Raster).
        /// </summary>
        private void PlaceSelectedPartAtMouse(Point mousePosition)
        {
            if (selectedPart == null)
                return;

            double posX;
            double posY;
            double posZ = 0.0;

            // --------------------------------------------------------
            // ERSTES BAUTEIL → fest in die Mitte des 2D-Editors
            // --------------------------------------------------------
            if (assembly.PlacedParts.Count == 0)
            {
                // Gewünschte Mittelposition im 2D-Editor
                posX = 550.0;
                posY = 275.0;

                // Auf das nächste Raster runden
                double grid = Grider.StepSize * Scale;
                posX = Math.Round(posX / grid) * grid;
                posY = Math.Round(posY / grid) * grid;
            }
            else
            {
                // --------------------------------------------------------
                // WEITERE BAUTEILE → wie bisher an der 3D-Klickposition
                // --------------------------------------------------------
                Point3D? hit = GetMousePointOnWorldPlane(mousePosition, 0.0);

                if (hit == null)
                {
                    StatusText.Text = "Klickpunkt nicht auf der Arbeitsebene";
                    return;
                }

                Point3D world = hit.Value;

                double centerXmm = world.X * 100.0;
                double centerYmm = -world.Y * 100.0;

                double halfCell = Grider.CellSize / 2.0;
                double grid = Grider.StepSize * Scale;

                posX = Math.Round((centerXmm - halfCell) * Scale / grid) * grid;
                posY = Math.Round((centerYmm - halfCell) * Scale / grid) * grid;
            }

            PlacedPart placed = new PlacedPart
            {
                Part = selectedPart,
                Transform = new PlastiCAD.Models.Transform
                {
                    Position = new Vector3(posX, posY, posZ)
                },
                Sockets = selectedPart.CreateSockets(),
                Rotation = 0
            };

            // Nächste freie Zelle, falls belegt
            if (!IsPositionFree(placed.Transform.Position, placed))
            {
                placed.Transform.Position =
                    FindNearestFreePosition(placed.Transform.Position, placed);
            }

            SaveUndoState();

            assembly.PlacedParts.Add(placed);

            selectedParts.Clear();
            selectedParts.Add(placed);

            int connections = ConnectSelectedParts();

            StatusText.Text = connections > 0
                ? $"Bauteil gesetzt – {connections} Verbindung(en)"
                : "Bauteil bei Z = 0 gesetzt";

            RedrawScene();
        }
        private void MainWindow_PreviewKeyDown(
     object sender,
     KeyEventArgs e)
        {


            bool controlPressed =
    (Keyboard.Modifiers & ModifierKeys.Control)
    == ModifierKeys.Control;



            if (e.Key == Key.Escape && isFullscreenAnimation)
            {
                StopFullscreenAnimation();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.F11)
            {
                if (isFullscreenAnimation)
                    StopFullscreenAnimation();
                else
                    StartFullscreenAnimation();

                e.Handled = true;
                return;
            }










            // Socket-Auswahl bestätigen
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                if (socketTargetPart != null && selectedPart != null)
                {
                    ConfirmSocketPlacement();
                    e.Handled = true;
                    return;
                }
            }

            // Socket-Auswahl abbrechen

            if (e.Key == Key.Add ||
   e.Key == Key.OemPlus)
            {
                currentPlanZ += Grider.StepSize;

                StatusText.Text =
                    $"Bearbeitungsebene Z = {currentPlanZ:0.##} mm";

                RedrawScene();

                e.Handled = true;
                return;
            }

            if (e.Key == Key.Subtract ||
                e.Key == Key.OemMinus)
            {
                currentPlanZ -= Grider.StepSize;

                StatusText.Text =
                    $"Bearbeitungsebene Z = {currentPlanZ:0.##} mm";

                RedrawScene();

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
                //  PartsList.SelectedIndex = -1;

                selectedParts.Clear();

                StatusText.Text = "Auswahlmodus";
                RedrawScene();

                e.Handled = true;
                return;
            }



            if (e.Key == Key.Delete)
            {
                DeleteSelection();

                e.Handled = true;
                return;
            }

            if (controlPressed && e.Key == Key.X)
            {
                CutSelection();
                e.Handled = true;
                return;
            }


            // ------------------------------------------------------------
            // RÜCKGÄNGIG / WIEDERHERSTELLEN
            // Unbedingt VOR Y und Z behandeln.
            // ------------------------------------------------------------

            if (controlPressed &&
                e.Key == Key.Z)
            {
                Undo();

                e.Handled = true;
                return;
            }

            if (controlPressed &&
                e.Key == Key.Y)
            {
                Redo();

                e.Handled = true;
                return;
            }


            if (e.Key == Key.Escape)
            {
                if (socketTargetPart != null)
                {
                    CancelSocketSelection();
                    e.Handled = true;
                    return;
                }

                // ... bestehende Escape-Logik ...

                {
                    selectedPart = null;

                    selectedParts.Clear();

                    if (selectedPartToolButton != null)
                    {
                        selectedPartToolButton.Background =
                            new SolidColorBrush(Color.FromRgb(244, 244, 244));

                        selectedPartToolButton.BorderBrush =
                            new SolidColorBrush(Color.FromRgb(181, 181, 181));

                        selectedPartToolButton.BorderThickness =
                            new Thickness(1);

                        selectedPartToolButton = null;
                    }

                    StatusText.Text = "Bereit";

                    RedrawScene();

                    e.Handled = true;
                    return;
                }
            }
            double moveStep = Grider.StepSize * Scale;


            if (controlPressed &&
                e.Key == Key.A)
            {
                selectedParts.Clear();

                foreach (PlacedPart placed in assembly.PlacedParts)
                {
                    selectedParts.Add(placed);
                }

                currentSnaps.Clear();

                StatusText.Text =
                    $"{selectedParts.Count} Bauteil(e) ausgewählt";

                RedrawScene();

                e.Handled = true;
                return;
            }

            if (selectedParts.Count == 0)
                return;

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


            if (selectedParts.Count == 0)
                return;


            bool is3DMode =
     MainTabs.SelectedItem == WorldTab;
            if (e.Key == Key.X)
            {
                if (is3DMode)
                    AnimateSelectionRotation('X');
                else
                    RotateSelection3D('X');

                e.Handled = true;
                return;
            }

            if (e.Key == Key.Y)
            {
                if (is3DMode)
                    AnimateSelectionRotation('Y');
                else
                    RotateSelection3D('Y');

                e.Handled = true;
                return;
            }

            if (e.Key == Key.Z)
            {
                if (is3DMode)
                    AnimateSelectionRotation('Z');
                else
                    RotateSelection3D('Z');

                e.Handled = true;
                return;
            }
            else if (e.Key == Key.Up)
            {
                SaveUndoState();

                foreach (PlacedPart placed in selectedParts)
                {
                    placed.Transform.Position.Z += Grider.StepSize;
                }

                RedrawScene();

                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                SaveUndoState();

                foreach (PlacedPart placed in selectedParts)
                {
                    placed.Transform.Position.Z -= Grider.StepSize;
                }

                RedrawScene();

                e.Handled = true;
            }
        }

        private void RotateSelection3D(char axis)
        {
            if (selectedParts.Count == 0)
                return;

            SaveUndoState();

            foreach (PlacedPart placed in selectedParts)
            {
                DisconnectPart(placed);
            }

            // ------------------------------------------------------------
            // TATSÄCHLICHE MITTELPUNKTE ERMITTELN
            // ------------------------------------------------------------
            Dictionary<PlacedPart, Vector3> actualPositions =
                new Dictionary<PlacedPart, Vector3>();

            foreach (PlacedPart placed in selectedParts)
            {
                Vector3 position = new Vector3(
                    placed.Transform.Position.X / Scale,
                    placed.Transform.Position.Y / Scale,
                    placed.Transform.Position.Z);

                if (placed.Part is Plate)
                {
                    Vector3 plateOffset = GetPlateGridOffset(placed);
                    position.X += plateOffset.X;
                    position.Y += plateOffset.Y;
                    position.Z += plateOffset.Z;
                }

                actualPositions[placed] = position;
            }

            // ------------------------------------------------------------
            // RASTERKONFORMER DREHPUNKT
            // ------------------------------------------------------------
            Vector3 pivot = GetRotationPivot(selectedParts);

            double centerX = pivot.X;
            double centerY = pivot.Y;
            double centerZ = pivot.Z;

            bool selectionContainsPlate =
                selectedParts.Any(p => p.Part is Plate);

            // ------------------------------------------------------------
            // BAUTEILE DREHEN
            // ------------------------------------------------------------
            foreach (PlacedPart placed in selectedParts)
            {
                Vector3 actualPosition = actualPositions[placed];

                Vector3 relativePosition = new Vector3(
                    actualPosition.X - centerX,
                    actualPosition.Y - centerY,
                    actualPosition.Z - centerZ);

                Vector3 rotatedPosition;

                switch (axis)
                {
                    case 'X':
                        rotatedPosition = relativePosition.RotateX90();

                        if (placed.Part is Plate)
                            RotatePlateOrientationWorld(placed, 'X');
                        else
                            placed.Transform.RotateWorldX90();
                        break;

                    case 'Y':
                        rotatedPosition = relativePosition.RotateY90();

                        if (placed.Part is Plate)
                            RotatePlateOrientationWorld(placed, 'Y');
                        else
                            placed.Transform.RotateWorldY90();
                        break;

                    case 'Z':
                        rotatedPosition = relativePosition.RotateZ90();

                        if (placed.Part is Plate)
                            RotatePlateOrientationWorld(placed, 'Z');
                        else
                            placed.Transform.RotateWorldZ90();
                        break;

                    default:
                        return;
                }

                // Neuer tatsächlicher Mittelpunkt nach der Drehung
                double newActualX = centerX + rotatedPosition.X;
                double newActualY = centerY + rotatedPosition.Y;
                double newActualZ = centerZ + rotatedPosition.Z;

                // --------------------------------------------------------
                // PLATTEN
                // --------------------------------------------------------
                if (placed.Part is Plate)
                {
                    Vector3 newPlateOffset = GetPlateGridOffset(placed);

                    placed.Transform.Position.X =
                        (newActualX - newPlateOffset.X) * Scale;

                    placed.Transform.Position.Y =
                        (newActualY - newPlateOffset.Y) * Scale;

                    placed.Transform.Position.Z =
                        newActualZ - newPlateOffset.Z;
                }
                // --------------------------------------------------------
                // NORMALE BAUTEILE
                // --------------------------------------------------------
                else
                {
                    // Weil der Pivot bereits rasterkonform ist,
                    // bleiben die relativen Abstände ebenfalls rasterkonform.
                    // Ein zusätzliches Runden ist nur noch als Sicherheitsnetz nötig.
                    if (!selectionContainsPlate)
                    {
                        double grid = Grider.StepSize;

                        double snappedX = Math.Round(newActualX / grid) * grid;
                        double snappedY = Math.Round(newActualY / grid) * grid;
                        double snappedZ = Math.Round(newActualZ / grid) * grid;

                        placed.Transform.Position.X = snappedX * Scale;
                        placed.Transform.Position.Y = snappedY * Scale;
                        placed.Transform.Position.Z = snappedZ;
                    }
                    else
                    {
                        // Bei gemischter Auswahl mit Platten nicht einzeln runden
                        placed.Transform.Position.X = newActualX * Scale;
                        placed.Transform.Position.Y = newActualY * Scale;
                        placed.Transform.Position.Z = newActualZ;
                    }
                }
            }

            int connectionCount = ConnectSelectedParts();

            StatusText.Text = connectionCount > 0
                ? $"{connectionCount} Verbindung(en)"
                : $"{selectedParts.Count} Bauteil(e) um {axis} gedreht";

            RedrawScene();
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

            double width = plate.Width / 100.0;
            double height = plate.Height / 100.0;
            double thickness = plate.Thickness / 100.0;

            Brush plateBrush =
                selectedParts.Contains(placed)
                    ? HighlightBrush(PaksyRed)
                    : PaksyRed;

            Point3D baseCenter = new Point3D(x, y, z);
            Point3D center;
            Vector3D holeNormal;
            Vector3D holeWidthAxis;
            double boxX, boxY, boxZ;

            switch (placed.PlateOrientation % 3)
            {
                case 1: // XZ, dünn in Y
                    center = new Point3D(
                        baseCenter.X + (Grider.CellSize / 2.0) / 100.0,
                        baseCenter.Y,
                        baseCenter.Z + (Grider.CellSize / 2.0) / 100.0);
                    boxX = width;
                    boxY = thickness;
                    boxZ = height;
                    holeNormal = new Vector3D(0, 1, 0);
                    holeWidthAxis = new Vector3D(1, 0, 0);
                    break;

                case 2: // YZ, dünn in X
                    center = new Point3D(
                        baseCenter.X,
                        baseCenter.Y - (Grider.CellSize / 2.0) / 100.0,
                        baseCenter.Z + (Grider.CellSize / 2.0) / 100.0);
                    boxX = thickness;
                    boxY = width;
                    boxZ = height;
                    holeNormal = new Vector3D(1, 0, 0);
                    holeWidthAxis = new Vector3D(0, 1, 0);
                    break;

                default: // XY, dünn in Z
                    center = new Point3D(
                        baseCenter.X + (Grider.CellSize / 2.0) / 100.0,
                        baseCenter.Y - (Grider.CellSize / 2.0) / 100.0,
                        baseCenter.Z);
                    boxX = width;
                    boxY = height;
                    boxZ = thickness;
                    holeNormal = new Vector3D(0, 0, 1);
                    holeWidthAxis = new Vector3D(1, 0, 0);
                    break;
            }

            if (plate is HolePlate holePlate)
            {
                GeometryModel3D holePlateModel = CreateRectangularPlateWithHole(
                    center,
                    holeNormal,
                    holeWidthAxis,
                    width,
                    height,
                    thickness,
                    (holePlate.HoleDiameter / 2.0) / 100.0,
                    plateBrush);

                if (holePlateModel != null)
                {
                    worldPartMap[holePlateModel] = placed;
                    WorldViewport.Children.Add(new ModelVisual3D { Content = holePlateModel }); 
                }
            }
            else
            {
                AddBox(center, boxX, boxY, boxZ, placed, plateBrush);
            }
        }
        private GeometryModel3D CreateRectangularPlateWithHole(
    Point3D center,
    Vector3D normal,
    Vector3D widthAxis,
    double width,
    double height,
    double thickness,
    double holeRadius,
    Brush brush)
        {
            const int segments = 48;

            if (normal.Length == 0 || widthAxis.Length == 0)
                return null;

            normal.Normalize();
            widthAxis.Normalize();

            Vector3D heightAxis = Vector3D.CrossProduct(normal, widthAxis);
            if (heightAxis.Length == 0)
                return null;
            heightAxis.Normalize();

            double hx = width / 2.0;
            double hy = height / 2.0;
            double hz = thickness / 2.0;

            MeshGeometry3D mesh = new MeshGeometry3D();

            // Innenkreis oben/unten
            for (int i = 0; i < segments; i++)
            {
                double a = 2.0 * Math.PI * i / segments;
                Vector3D radial =
                    widthAxis * Math.Cos(a) * holeRadius +
                    heightAxis * Math.Sin(a) * holeRadius;

                mesh.Positions.Add(center + radial + normal * hz); // oben innen
                mesh.Positions.Add(center + radial - normal * hz); // unten innen
            }

            // Außenrechteck: Strahl Kreiswinkel → Quadrat
            for (int i = 0; i < segments; i++)
            {
                double a = 2.0 * Math.PI * i / segments;
                double cx = Math.Cos(a);
                double cy = Math.Sin(a);

                double scale = Math.Min(
                    hx / Math.Max(Math.Abs(cx), 1e-6),
                    hy / Math.Max(Math.Abs(cy), 1e-6));

                Vector3D radial = widthAxis * (cx * scale) + heightAxis * (cy * scale);

                mesh.Positions.Add(center + radial + normal * hz); // oben außen
                mesh.Positions.Add(center + radial - normal * hz); // unten außen
            }

            int innerTop(int i) => (i % segments) * 2;
            int innerBot(int i) => (i % segments) * 2 + 1;
            int outerTop(int i) => segments * 2 + (i % segments) * 2;
            int outerBot(int i) => segments * 2 + (i % segments) * 2 + 1;

            for (int i = 0; i < segments; i++)
            {
                int n = i + 1;

                // Deckfläche oben
                mesh.TriangleIndices.Add(innerTop(i));
                mesh.TriangleIndices.Add(outerTop(i));
                mesh.TriangleIndices.Add(outerTop(n));
                mesh.TriangleIndices.Add(innerTop(i));
                mesh.TriangleIndices.Add(outerTop(n));
                mesh.TriangleIndices.Add(innerTop(n));

                // Deckfläche unten
                mesh.TriangleIndices.Add(innerBot(i));
                mesh.TriangleIndices.Add(outerBot(n));
                mesh.TriangleIndices.Add(outerBot(i));
                mesh.TriangleIndices.Add(innerBot(i));
                mesh.TriangleIndices.Add(innerBot(n));
                mesh.TriangleIndices.Add(outerBot(n));

                // Lochwand
                mesh.TriangleIndices.Add(innerTop(i));
                mesh.TriangleIndices.Add(innerTop(n));
                mesh.TriangleIndices.Add(innerBot(n));
                mesh.TriangleIndices.Add(innerTop(i));
                mesh.TriangleIndices.Add(innerBot(n));
                mesh.TriangleIndices.Add(innerBot(i));

                // Außenwand
                mesh.TriangleIndices.Add(outerTop(i));
                mesh.TriangleIndices.Add(outerBot(i));
                mesh.TriangleIndices.Add(outerBot(n));
                mesh.TriangleIndices.Add(outerTop(i));
                mesh.TriangleIndices.Add(outerBot(n));
                mesh.TriangleIndices.Add(outerTop(n));
            }

            DiffuseMaterial material = new DiffuseMaterial(brush);
            return new GeometryModel3D
            {
                Geometry = mesh,
                Material = material,
                BackMaterial = material
            };
        }
        private Vector3 GetPlateGridOffset(
    PlacedPart placed)
        {
            double half =
                Grider.CellSize / 2.0;

            switch (placed.PlateOrientation % 3)
            {
                case 0:
                    return new Vector3(
                        half,
                        half,
                        0);

                case 1:
                    return new Vector3(
                        half,
                        0,
                        half);

                case 2:
                    return new Vector3(
                        0,
                        half,
                        half);

                default:
                    return new Vector3();
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

            Brush yBrush =
    new SolidColorBrush(
        Color.FromArgb(
            180,
            255,
            190,
            40));

            double minY =
                planeY - cellRadius * cellSize;

            double maxY =
                planeY + cellRadius * cellSize;

            GeometryModel3D yLine =
                CreateLine3DModel(
                    new Point3D(
                        referenceX,
                        minY,
                        referenceZ),

                    new Point3D(
                        referenceX,
                        maxY,
                        referenceZ),

                    lineRadius * 2.5,
                    yBrush);

            if (yLine != null)
            {
                group.Children.Add(yLine);
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

        private void PartToolButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            if (!(sender is Button button))
                return;

            string partName =
                button.Tag as string;

            if (string.IsNullOrWhiteSpace(partName))
                return;

            Part part =
                PartLibrary.Parts.FirstOrDefault(
                    item => item.Name == partName);

            if (part == null)
            {
                StatusText.Text =
                    $"Bauteil nicht gefunden: {partName}";

                return;
            }

            selectedPart = part;

            StatusText.Text =
                "Ausgewählt: " + selectedPart.Name;

            UpdatePartToolSelection(button);
        }

        private void UpdatePartToolSelection(
    Button selectedButton)
        {
            if (selectedPartToolButton != null)
            {
                selectedPartToolButton.Background =
                    new SolidColorBrush(
                        Color.FromRgb(
                            244,
                            244,
                            244));

                selectedPartToolButton.BorderBrush =
                    new SolidColorBrush(
                        Color.FromRgb(
                            181,
                            181,
                            181));

                selectedPartToolButton.BorderThickness =
                    new Thickness(1.0);
            }

            selectedPartToolButton =
                selectedButton;

            selectedPartToolButton.Background =
                new SolidColorBrush(
                    Color.FromRgb(
                        204,
                        232,
                        245));

            selectedPartToolButton.BorderBrush =
                new SolidColorBrush(
                    Color.FromRgb(
                        0,
                        120,
                        180));

            selectedPartToolButton.BorderThickness =
                new Thickness(2.0);
        }

        private void RotatePlateOrientationWorld(
    PlacedPart placed,
    char axis)
        {
            if (!(placed.Part is Plate))
                return;

            bool supportsFrontAndBack =
                placed.Part is BigPlate;

            int plane =
                placed.PlateOrientation % 3;

            bool isFlipped =
                supportsFrontAndBack &&
                placed.PlateOrientation >= 3;


            // ------------------------------------------------------------
            // WICHTIG:
            // Bei der Streifenplatte die ALTEN Streifenachsen merken,
            // BEVOR PlateOrientation verändert wird.
            // ------------------------------------------------------------

            Vector3D oldAcross = new Vector3D();
            Vector3D oldStack = new Vector3D();

            bool isSlatPlate =
                placed.Part is SlatPlate;

            if (isSlatPlate)
            {
                GetSlatAxes(
                    placed,
                    out oldAcross,
                    out oldStack,
                    out _);
            }


            // ------------------------------------------------------------
            // NORMALE DER PLATTE
            // ------------------------------------------------------------

            Vector3 normal;

            switch (plane)
            {
                case 0:
                    // XY
                    normal =
                        new Vector3(0, 0, 1);
                    break;

                case 1:
                    // XZ
                    normal =
                        new Vector3(0, 1, 0);
                    break;

                case 2:
                    // YZ
                    normal =
                        new Vector3(1, 0, 0);
                    break;

                default:
                    return;
            }

            if (isFlipped)
            {
                normal =
                    new Vector3(
                        -normal.X,
                        -normal.Y,
                        -normal.Z);
            }


            // ------------------------------------------------------------
            // PLATTE DREHEN
            // ------------------------------------------------------------

            switch (axis)
            {
                case 'X':
                    normal =
                        normal.RotateX90();
                    break;

                case 'Y':
                    normal =
                        normal.RotateY90();
                    break;

                case 'Z':
                    normal =
                        normal.RotateZ90();
                    break;

                default:
                    return;
            }


            // Neue Plattenebene setzen
            placed.PlateOrientation =
                GetPlateOrientationFromNormal(
                    normal,
                    supportsFrontAndBack);


            // ------------------------------------------------------------
            // NORMALE PLATTE:
            // hier sind wir fertig
            // ------------------------------------------------------------

            if (!isSlatPlate)
                return;


            // ------------------------------------------------------------
            // STREIFENPLATTE
            //
            // Jetzt die ALTEN Streifenachsen mit derselben
            // Weltrotation drehen wie die Platte.
            // ------------------------------------------------------------

            Vector3D wantedAcross;
            Vector3D wantedStack;

            switch (axis)
            {
                case 'X':
                    wantedAcross =
                        RotateVectorX90(oldAcross);

                    wantedStack =
                        RotateVectorX90(oldStack);
                    break;

                case 'Y':
                    wantedAcross =
                        RotateVectorY90(oldAcross);

                    wantedStack =
                        RotateVectorY90(oldStack);
                    break;

                case 'Z':
                    wantedAcross =
                        RotateVectorZ90(oldAcross);

                    wantedStack =
                        RotateVectorZ90(oldStack);
                    break;

                default:
                    return;
            }


            // ------------------------------------------------------------
            // Prüfen, welche der zwei möglichen Streifenrichtungen
            // in der NEUEN Plattenebene der echten gedrehten Richtung
            // entspricht.
            // ------------------------------------------------------------

            placed.Transform.Rotation.Z = 0;

            GetSlatAxes(
                placed,
                out Vector3D across0,
                out Vector3D stack0,
                out _);

            double score0 =
                Math.Abs(
                    Vector3D.DotProduct(
                        across0,
                        wantedAcross))
                +
                Math.Abs(
                    Vector3D.DotProduct(
                        stack0,
                        wantedStack));


            placed.Transform.Rotation.Z = 90;

            GetSlatAxes(
                placed,
                out Vector3D across90,
                out Vector3D stack90,
                out _);

            double score90 =
                Math.Abs(
                    Vector3D.DotProduct(
                        across90,
                        wantedAcross))
                +
                Math.Abs(
                    Vector3D.DotProduct(
                        stack90,
                        wantedStack));


            placed.Transform.Rotation.Z =
                score90 > score0
                    ? 90
                    : 0;
        }
        private int GetPlateOrientationFromNormal(
    Vector3 normal,
    bool supportsFrontAndBack)
        {
            const double tolerance = 0.001;

            // XY-Ebene
            if (Math.Abs(normal.Z) > 1.0 - tolerance)
            {
                if (supportsFrontAndBack &&
                    normal.Z < 0)
                {
                    return 3;
                }

                return 0;
            }

            // XZ-Ebene
            // Standard-Vorderseite zeigt nach -Y.
            // XZ-Ebene
            // Standard-Vorderseite zeigt in Paksy +Y.
            if (Math.Abs(normal.Y) > 1.0 - tolerance)
            {
                // -Y bedeutet Rückseite
                if (supportsFrontAndBack &&
                    normal.Y < 0)
                {
                    return 4;
                }

                return 1;
            }

            // YZ-Ebene
            // Standard-Vorderseite zeigt nach +X.
            if (Math.Abs(normal.X) > 1.0 - tolerance)
            {
                if (supportsFrontAndBack &&
                    normal.X < 0)
                {
                    return 5;
                }

                return 2;
            }

            return 0;
        }

        private Point GetPartPlanPosition(
    PlacedPart placed)
        {
            double x =
                placed.Transform.Position.X;

            double y =
                placed.Transform.Position.Y;

            // Platten liegen im Plan zwischen den Rasterpunkten.
            if (placed.Part is Plate)
            {
                Vector3 offset =
                     GetPlateGridOffset(placed);

                x += offset.X * Scale;
                y += offset.Y * Scale;
            }

            return new Point(
                x,
                y);
        }

        private void AnimateSelectionRotation(char axis)
        {
            if (selectedParts.Count == 0)
                return;

            if (isSelectionRotationAnimating)
                return;

            isSelectionRotationAnimating = true;

            selectionRotationAxis = axis;
            selectionRotationStep = 0;

            selectionRotationOriginalTransforms.Clear();

            // ------------------------------------------------------------
            // RASTERKONFORMER PIVOT (identisch zu RotateSelection3D)
            // ------------------------------------------------------------
            Vector3 pivot = GetRotationPivot(selectedParts);

            double centerX = pivot.X;
            double centerY = pivot.Y;
            double centerZ = pivot.Z;

            // ------------------------------------------------------------
            // PAKSY-KOORDINATEN → WPF-WELTKOORDINATEN
            // ------------------------------------------------------------
            double halfGrid = Grider.CellSize / 2.0;

            selectionRotationPivot = new Point3D(
                (centerX + halfGrid) / 100.0,
                -(centerY + halfGrid) / 100.0,
                centerZ / 100.0);

            // ------------------------------------------------------------
            // 3D-MODELLE DER AUSWAHL MERKEN
            // ------------------------------------------------------------
            foreach (KeyValuePair<Model3D, PlacedPart> entry in worldPartMap)
            {
                if (!selectedParts.Contains(entry.Value))
                    continue;

                selectionRotationOriginalTransforms[entry.Key] = entry.Key.Transform;
            }

            // ------------------------------------------------------------
            // ANIMATION STARTEN
            // ------------------------------------------------------------
            selectionRotationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(7)
            };

            selectionRotationTimer.Tick += SelectionRotationTimer_Tick;
            selectionRotationTimer.Start();
        }
        private void SelectionRotationTimer_Tick(
    object sender,
    EventArgs e)
        {
            selectionRotationStep++;

            double progress =
                (double)selectionRotationStep
                / SelectionRotationSteps;

            // 0 bis 90 Grad
            double angle =
                90.0 * progress;

            ApplySelectionRotationPreview(
                selectionRotationAxis,
                angle);

            if (selectionRotationStep <
                SelectionRotationSteps)
            {
                return;
            }

            selectionRotationTimer.Stop();

            selectionRotationTimer.Tick -=
                SelectionRotationTimer_Tick;

            selectionRotationTimer = null;

            // Preview-Transformationen wieder entfernen.
            foreach (KeyValuePair<Model3D, Transform3D> entry
                     in selectionRotationOriginalTransforms)
            {
                entry.Key.Transform =
                    entry.Value;
            }

            selectionRotationOriginalTransforms.Clear();

            isSelectionRotationAnimating = false;

            // Jetzt erst die echten CAD-Daten exakt um 90° drehen.
            RotateSelection3D(
                selectionRotationAxis);
        }

        private void ApplySelectionRotationPreview(
    char axis,
    double angle)
        {
            Vector3D worldAxis;

            double worldAngle =
                angle;

            switch (axis)
            {
                case 'X':
                    worldAxis =
                        new Vector3D(
                            1,
                            0,
                            0);
                    break;

                case 'Y':
                    worldAxis =
                        new Vector3D(
                            0,
                            1,
                            0);
                    break;

                case 'Z':
                    worldAxis =
                        new Vector3D(
                            0,
                            0,
                            1);

                    // Paksy-Y ist gegenüber World-Y gespiegelt.
                    // Deshalb Z in der Gegenrichtung drehen.
                    worldAngle =
                        -angle;

                    break;

                default:
                    return;
            }

            AxisAngleRotation3D rotation =
                new AxisAngleRotation3D(
                    worldAxis,
                    worldAngle);

            RotateTransform3D previewTransform =
                new RotateTransform3D(
                    rotation,
                    selectionRotationPivot);

            foreach (KeyValuePair<Model3D, Transform3D> entry
                     in selectionRotationOriginalTransforms)
            {
                Transform3DGroup group =
                    new Transform3DGroup();

                if (entry.Value != null)
                {
                    group.Children.Add(
                        entry.Value);
                }

                group.Children.Add(
                    previewTransform);

                entry.Key.Transform =
                    group;
            }
        }


        private void MainTabs_SelectionChanged(
    object sender,
    SelectionChangedEventArgs e)
        {
            if (PlanToolbar == null ||
                WorldToolbar == null)
                return;

            bool worldIsActive =
                MainTabs.SelectedItem == WorldTab;

            if (worldIsActive)
            {
                PlanToolbar.Visibility =
                    Visibility.Collapsed;

                WorldToolbar.Visibility =
                    Visibility.Visible;

                RedrawWorld();
            }
            else
            {
                PlanToolbar.Visibility =
                    Visibility.Visible;

                WorldToolbar.Visibility =
                    Visibility.Collapsed;

                RedrawPlan();
            }
        }
        private void UpdateWindowTitle()
        {
            string name = string.IsNullOrEmpty(currentProjectFileName)
                ? "Neues Projekt"
                : System.IO.Path.GetFileName(currentProjectFileName);

            Title = (isProjectDirty ? "* " : "") + name + " - PlastiCAD";
        }

        private void MainWindow_SizeChanged(
    object sender,
    SizeChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            RedrawScene();
        }


        private void RedrawPlan()
        {
            // ------------------------------------------------------------
            // ALLE NICHT-RASTERELEMENTE ENTFERNEN
            // ------------------------------------------------------------

            for (int i = BuildArea.Children.Count - 1;
                 i >= 0;
                 i--)
            {
                FrameworkElement element =
                    BuildArea.Children[i]
                    as FrameworkElement;

                if (element == null)
                    continue;

                // Raster bleibt erhalten
                if ((element.Tag as string) != "Grid")
                {
                    BuildArea.Children.RemoveAt(i);
                }
            }

            // ------------------------------------------------------------
            // RASTER NUR ERZEUGEN, WENN NOCH KEINS VORHANDEN IST
            // ------------------------------------------------------------

            bool gridExists =
                BuildArea.Children
                    .OfType<FrameworkElement>()
                    .Any(element =>
                        (element.Tag as string) == "Grid");

            if (!gridExists)
            {
                DrawGrid();
            }

            // ------------------------------------------------------------
            // Ab hier dein bisheriger Bauteil-Zeichencode
            // ------------------------------------------------------------

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

                if (placed.Part is BigPlate bigPlate)
                {
                    DrawBigPlate2D(
                        placed,
                        bigPlate);

                    continue;
                }
                if (placed.Part is SlatPlate slatPlate)
                {
                    DrawSlatPlate2D(placed, slatPlate);
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
                    placed.Part is BigPlate ||
                    placed.Part is Plate)
                {
                    continue;
                }
                if (placed.Part is BigWheel bigWheel)
                {
                    DrawBigWheel2D(
                        placed,
                        bigWheel);

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
        }
        private void GetSlatAxes(
    PlacedPart placed,
    out Vector3D across,
    out Vector3D stack,
    out Vector3D thick)
        {
            switch (placed.PlateOrientation % 3)
            {
                case 1:
                    across = new Vector3D(1, 0, 0);
                    stack = new Vector3D(0, 0, 1);
                    thick = new Vector3D(0, 1, 0);
                    break;

                case 2:
                    across = new Vector3D(0, 1, 0);
                    stack = new Vector3D(0, 0, 1);
                    thick = new Vector3D(1, 0, 0);
                    break;

                default:
                    across = new Vector3D(1, 0, 0);
                    stack = new Vector3D(0, 1, 0);
                    thick = new Vector3D(0, 0, 1);
                    break;
            }

            if (placed.Part is SlatPlate &&
                Math.Abs(placed.Transform.Rotation.Z) >= 45)
            {
                Vector3D oldAcross = across;
                across = stack;
                stack = new Vector3D(-oldAcross.X, -oldAcross.Y, -oldAcross.Z);
            }
        }
        private void DrawSlatPlate2D(PlacedPart placed, SlatPlate plate)
        {
            bool isCurrentLayer =
                Math.Abs(placed.Transform.Position.Z - currentPlanZ) < 0.001;

            Brush brush = selectedParts.Contains(placed)
                ? HighlightBrush(PaksyYellow)
                : isCurrentLayer
                    ? PaksyYellow
                    : new SolidColorBrush(Color.FromArgb(70, 245, 190, 35));

            double halfGrid = Grider.CellSize * Scale / 2.0;
            Vector3 cellCenter = GetCellCenter(placed);

            double centerX = cellCenter.X + halfGrid;
            double centerY = cellCenter.Y + halfGrid;
            if (placed.PlateOrientation % 3 == 1)
                centerY = cellCenter.Y;
            if (placed.PlateOrientation % 3 == 2)
                centerX = cellCenter.X;

            GetSlatAxes(placed, out Vector3D across, out Vector3D stack, out Vector3D thick);

            double plateW = plate.Width * Scale;
            double rPx = (plate.GutterDiameter / 2.0) * Scale;
            double[] slats = plate.GetSlatWidths();
            double gapPx = plate.GapWidth * Scale;
            double totalPx =
                (plate.OuterSlatWidth * 2
                 + plate.InnerSlatWidth * 2
                 + plate.GapWidth * 3) * Scale;

            bool topView = Math.Abs(thick.Z) >= Math.Abs(stack.Z) &&
                           Math.Abs(thick.Z) >= Math.Abs(across.Z);
            bool profileView = Math.Abs(stack.Z) >= Math.Abs(across.Z) &&
                               Math.Abs(stack.Z) >= Math.Abs(thick.Z);

            if (topView)
            {
                bool stackAlongX = Math.Abs(stack.X) >= Math.Abs(stack.Y);
                double cursor = -totalPx / 2.0;

                for (int i = 0; i < slats.Length; i++)
                {
                    double slatPx = slats[i] * Scale;
                    Rectangle slat = new Rectangle
                    {
                        Width = stackAlongX ? slatPx : plateW,
                        Height = stackAlongX ? plateW : slatPx,
                        Fill = brush
                    };

                    if (stackAlongX)
                    {
                        Canvas.SetLeft(slat, centerX + cursor);
                        Canvas.SetTop(slat, centerY - slat.Height / 2.0);
                    }
                    else
                    {
                        Canvas.SetLeft(slat, centerX - slat.Width / 2.0);
                        Canvas.SetTop(slat, centerY + cursor);
                    }

                    BuildArea.Children.Add(slat);
                    cursor += slatPx + gapPx;
                }

                if (stackAlongX)
                {
                    AddGutterRect(centerX - totalPx / 2.0, centerY - plateW / 2.0 - rPx, totalPx, rPx, brush);
                    AddGutterRect(centerX - totalPx / 2.0, centerY + plateW / 2.0, totalPx, rPx, brush);
                }
                else
                {
                    AddGutterRect(centerX - plateW / 2.0 - rPx, centerY - totalPx / 2.0, rPx, totalPx, brush);
                    AddGutterRect(centerX + plateW / 2.0, centerY - totalPx / 2.0, rPx, totalPx, brush);
                }

                return;
            }

            if (profileView)
            {
                // Seitenansicht wie Skizze: Stab + 90°-Rinnen nach außen
                bool alongX = Math.Abs(across.X) >= Math.Abs(across.Y);
                double slatW = alongX ? plateW : Math.Max(2.0, plate.Thickness * Scale);
                double slatH = alongX ? Math.Max(2.0, plate.Thickness * Scale) : plateW;

                Rectangle bar = new Rectangle
                {
                    Width = slatW,
                    Height = slatH,
                    Fill = brush
                };
                Canvas.SetLeft(bar, centerX - slatW / 2.0);
                Canvas.SetTop(bar, centerY - slatH / 2.0);
                BuildArea.Children.Add(bar);

                if (alongX)
                {
                    AddGutterArc(centerX - plateW / 2.0 - rPx, centerY, rPx, -45, brush);
                    AddGutterArc(centerX + plateW / 2.0 + rPx, centerY, rPx, 135, brush);
                }
                else
                {
                    AddGutterArc(centerX, centerY - plateW / 2.0 - rPx, rPx, 45, brush);
                    AddGutterArc(centerX, centerY + plateW / 2.0 + rPx, rPx, 225, brush);
                }

                return;
            }
            // Andere Seite: nur Rechteck, hochkant wenn die Stäbe in Y liegen
            double longPx = totalPx;
            double shortPx = Math.Max(2.0, plate.Thickness * Scale + rPx);
            bool tall = Math.Abs(stack.Y) >= Math.Abs(stack.X);

            Rectangle side = new Rectangle
            {
                Width = tall ? shortPx : longPx,
                Height = tall ? longPx : shortPx,
                Fill = brush
            };
            Canvas.SetLeft(side, centerX - side.Width / 2.0);
            Canvas.SetTop(side, centerY - side.Height / 2.0);
            BuildArea.Children.Add(side);
        }
        private void AddGutterRect(double left, double top, double width, double height, Brush brush)
        {
            Rectangle rect = new Rectangle
            {
                Width = width,
                Height = height,
                Fill = brush
            };
            Canvas.SetLeft(rect, left);
            Canvas.SetTop(rect, top);
            BuildArea.Children.Add(rect);
        }

        private void AddGutterArc(double cx, double cy, double rPx, double startDeg, Brush brush)
        {
            double a0 = startDeg * Math.PI / 180.0;
            double a1 = (startDeg + 90.0) * Math.PI / 180.0;

            PathFigure fig = new PathFigure
            {
                StartPoint = new Point(cx + rPx * Math.Cos(a0), cy + rPx * Math.Sin(a0)),
                IsClosed = false
            };
            fig.Segments.Add(new ArcSegment
            {
                Point = new Point(cx + rPx * Math.Cos(a1), cy + rPx * Math.Sin(a1)),
                Size = new Size(rPx, rPx),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = false
            });

            BuildArea.Children.Add(new System.Windows.Shapes.Path
            {
                Stroke = brush,
                StrokeThickness = 2.0,
                Data = new PathGeometry(new[] { fig })
            });
        }
        private void DrawSlatPlate3D(PlacedPart placed, SlatPlate plate)
        {
            double x = (placed.Transform.Position.X / Scale + Grider.CellSize / 2.0) / 100.0;
            double y = -(placed.Transform.Position.Y / Scale + Grider.CellSize / 2.0) / 100.0;
            double z = placed.Transform.Position.Z / 100.0;

            Point3D origin;
            switch (placed.PlateOrientation % 3)
            {
                case 1:
                    origin = new Point3D(
                        x + (Grider.CellSize / 2.0) / 100.0,
                        y,
                        z + (Grider.CellSize / 2.0) / 100.0);
                    break;

                case 2:
                    origin = new Point3D(
                        x,
                        y - (Grider.CellSize / 2.0) / 100.0,
                        z + (Grider.CellSize / 2.0) / 100.0);
                    break;

                default:
                    origin = new Point3D(
                        x + (Grider.CellSize / 2.0) / 100.0,
                        y - (Grider.CellSize / 2.0) / 100.0,
                        z);
                    break;
            }

            GetSlatAxes(placed, out Vector3D across, out Vector3D stack, out Vector3D thick);

            Brush brush = selectedParts.Contains(placed)
                ? HighlightBrush(PaksyYellow)
                : PaksyYellow;

            double w = plate.Width / 100.0;
            double t = plate.Thickness / 100.0;
            double radius = (plate.GutterDiameter / 2.0) / 100.0;
            double attach = radius * Math.Cos(Math.PI / 4.0);
            double slatLength = w;

            double[] slats = plate.GetSlatWidths();
            double totalMm =
                plate.OuterSlatWidth * 2
                + plate.InnerSlatWidth * 2
                + plate.GapWidth * 3;

            double cursor = -totalMm / 2.0;

            foreach (double slatMm in slats)
            {
                double slat = slatMm / 100.0;
                Point3D center = origin + stack * ((cursor + slatMm / 2.0) / 100.0);

                AddBox(
                    center,
                    Math.Abs(across.X) * slatLength + Math.Abs(stack.X) * slat + Math.Abs(thick.X) * t,
                    Math.Abs(across.Y) * slatLength + Math.Abs(stack.Y) * slat + Math.Abs(thick.Y) * t,
                    Math.Abs(across.Z) * slatLength + Math.Abs(stack.Z) * slat + Math.Abs(thick.Z) * t,
                    placed,
                    brush);

                cursor += slatMm + plate.GapWidth;
            }

            double gutterLength = totalMm / 100.0;
            double mid = Math.PI / 4.0;
            double axisOffset = w / 2.0 + radius;

            for (int side = -1; side <= 1; side += 2)
            {
                Vector3D outward = across * side;
                Point3D gutterCenter = origin + outward * axisOffset;

                GeometryModel3D gutter = CreateQuarterCylinder(
                    gutterCenter,
                    stack,
                    outward,
                    thick,
                    radius,
                    totalMm / 100.0,
                    brush);

                if (gutter != null)
                {
                    worldPartMap[gutter] = placed;
                    WorldViewport.Children.Add(new ModelVisual3D { Content = gutter });
                }
            }
        }

        private static Vector3D RotateVectorX90(Vector3D v) => new Vector3D(v.X, v.Z, -v.Y);
        private static Vector3D RotateVectorY90(Vector3D v) => new Vector3D(v.Z, v.Y, -v.X);
        private static Vector3D RotateVectorZ90(Vector3D v) => new Vector3D(-v.Y, v.X, v.Z);
        private void RedrawScene()
        {
            if (MainTabs.SelectedItem == WorldTab)
            {
                RedrawWorld();
            }
            else
            {
                RedrawPlan();
            }
        }

        private void DrawBigWheel3D(
    PlacedPart placed,
    BigWheel wheel)
        {
            // ------------------------------------------------------------
            // POSITION UND AUSRICHTUNG
            // Genau wie beim kleinen Rad
            // ------------------------------------------------------------

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


            Face wheelFace =
                FaceHelper.RotateFace(
                    Face.Right,
                    placed.Rotation);

            Vector3 direction =
                GetDirectionFromFace(
                    wheelFace);

            direction =
                placed.Transform.ApplyRotation(
                    direction);


            // ------------------------------------------------------------
            // RADMITTELPUNKT
            // ------------------------------------------------------------

            double halfTotalWidth =
                wheel.Width / 200.0;

            double armEndDistance =
                (Grider.CellSize / 2.0)
                / 100.0;

            double wheelCenterDistance =
                armEndDistance
                - halfTotalWidth;

            Point3D wheelCenter =
                new Point3D(
                    cellCenter.X
                        + direction.X
                        * wheelCenterDistance,

                    cellCenter.Y
                        - direction.Y
                        * wheelCenterDistance,

                    cellCenter.Z
                        + direction.Z
                        * wheelCenterDistance);


            Brush rimBrush =
                PaksyRed;

            Brush tireBrush =
                Brushes.Black;

            if (selectedParts.Contains(placed))
            {
                rimBrush =
                    HighlightBrush(rimBrush);

                tireBrush =
                    new SolidColorBrush(
                        Color.FromRgb(
                            70,
                            70,
                            70));
            }


            // ------------------------------------------------------------
            // 1. MASSIVER SCHWARZER REIFEN
            //
            // außen Ø64
            // innen Ø46
            // Breite 8 mm
            // ------------------------------------------------------------

            double tireOuterRadius =
                wheel.OuterDiameter
                / 200.0;

            double tireInnerRadius =
                wheel.RimDiameter
                / 200.0;

            double tireHalfWidth =
                wheel.TireWidth
                / 200.0;

            AddFlatRing(
                wheelCenter,
                direction,

                tireOuterRadius,
                tireInnerRadius,

                tireHalfWidth,

                placed,
                tireBrush);


            // ------------------------------------------------------------
            // 2. FLACHE ROTE FELGENSCHEIBE
            //
            // Ø46
            // nur etwa 2 mm dick
            // ------------------------------------------------------------

            double rimOuterRadius =
                wheel.RimDiameter
                / 200.0;

            double centerHoleRadius =
                wheel.HoleDiameter
                / 200.0;

            double rimBodyHalfWidth =
                wheel.RimBodyThickness
                / 200.0;

            AddFlatRing(
                wheelCenter,
                direction,

                rimOuterRadius,
                centerHoleRadius,

                rimBodyHalfWidth,

                placed,
                rimBrush);


            // ------------------------------------------------------------
            // 3. ÄUSSERER FELGENRAND
            //
            // radial etwa 2 mm breit
            // etwas kräftiger als die flache Scheibe
            // ------------------------------------------------------------

            double rimEdgeInnerRadius =
                (wheel.RimDiameter / 2.0
                - wheel.RimEdgeWidth)
                / 100.0;

            double rimEdgeHalfWidth =
                2.0 / 100.0;

            AddFlatRing(
                wheelCenter,
                direction,

                rimOuterRadius,
                rimEdgeInnerRadius,

                rimEdgeHalfWidth,

                placed,
                rimBrush);


            // ------------------------------------------------------------
            // 4. MITTLERE NABE
            //
            // Bohrung Ø9,5 mm
            // Gesamtlänge 9 mm
            //
            // Außendurchmesser ist vorerst 14 mm.
            // Den können wir optisch noch korrigieren.
            // ------------------------------------------------------------

            double hubOuterRadius =
                7.0 / 100.0;

            double hubHalfWidth =
                wheel.BoreDepth
                / 200.0;

            AddFlatRing(
                wheelCenter,
                direction,

                hubOuterRadius,
                centerHoleRadius,

                hubHalfWidth,

                placed,
                rimBrush);


            // ------------------------------------------------------------
            // BASISVEKTOREN IN DER RADEBENE
            // ------------------------------------------------------------

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


            // ------------------------------------------------------------
            // 5. VIER ZUSÄTZLICHE Ø9,5-mm-LÖCHER
            //
            // Zunächst optisch als schwarze Bohrungen.
            // ------------------------------------------------------------

            double sideHoleRadius =
                wheel.SideHoleRadius
                / 100.0;

            double sideHoleTubeRadius =
                wheel.SideHoleDiameter
                / 200.0;

            double holeHalfLength =
                2.5 / 100.0;


            for (int i = 0;
     i < wheel.SideHoleCount;
     i++)
            {
                double angle =
                    Math.PI / 4.0
                    + i * Math.PI / 2.0;

                Vector3D radial =
                    side1 * Math.Cos(angle)
                    + side2 * Math.Sin(angle);

                Point3D holeCenter =
                    wheelCenter
                    + radial
                    * sideHoleRadius;

                Point3D holeStart =
                    holeCenter
                    - axis
                    * holeHalfLength;

                Point3D holeEnd =
                    holeCenter
                    + axis
                    * holeHalfLength;

                AddCylinder(
                    holeStart,
                    holeEnd,
                    sideHoleTubeRadius,
                    placed,
                    Brushes.Black);
            }


            // ------------------------------------------------------------
            // 6. VIER RUNDFUGEN
            //
            // je ca. 60°
            // 2 mm breit
            // etwa 3 mm vor Felgenrand
            // ------------------------------------------------------------

            double grooveRadius =
                (
                    wheel.RimDiameter / 2.0
                    - wheel.GrooveInset
                    - wheel.GrooveWidth / 2.0
                ) / 100.0;

            double grooveTubeRadius =
                wheel.GrooveWidth
                / 2.0
                / 100.0;


            // Vorderseite der Felge
            double frontOffset =
                rimBodyHalfWidth
                + grooveTubeRadius * 0.25;

            Point3D grooveFaceCenter =
                wheelCenter
                - axis * frontOffset;


            for (int i = 0;
                 i < wheel.GrooveCount;
                 i++)
            {
                // 4 Fugen um jeweils 90° verteilt.
                // Jede Fuge selbst ist 60° lang.
                double middleAngle =
                    i * 90.0;

                double startAngle =
                    middleAngle
                    - wheel.GrooveAngle / 2.0;

                AddArcTube(
                    grooveFaceCenter,
                    axis,
                    side1,
                    side2,

                    grooveRadius,
                    grooveTubeRadius,

                    startAngle,
                    wheel.GrooveAngle,

                    placed,

                    Brushes.DarkRed);
            }
        }

        private void AddFlatRing(
    Point3D center,
    Vector3 direction,
    double outerRadius,
    double innerRadius,
    double halfWidth,
    PlacedPart placed,
    Brush brush)
        {
            const int segments = 64;

            Vector3D axis =
                new Vector3D(
                    direction.X,
                    -direction.Y,
                    direction.Z);

            if (axis.Length == 0)
                return;

            if (outerRadius <= innerRadius ||
                innerRadius < 0 ||
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


            MeshGeometry3D mesh =
                new MeshGeometry3D();


            // Pro Segment:
            //
            // 0 = vorne außen
            // 1 = vorne innen
            // 2 = hinten außen
            // 3 = hinten innen

            for (int i = 0;
                 i < segments;
                 i++)
            {
                double angle =
                    2.0 * Math.PI
                    * i
                    / segments;

                Vector3D radial =
                    side1 * Math.Cos(angle)
                    + side2 * Math.Sin(angle);

                mesh.Positions.Add(
                    center
                    - axis * halfWidth
                    + radial * outerRadius);

                mesh.Positions.Add(
                    center
                    - axis * halfWidth
                    + radial * innerRadius);

                mesh.Positions.Add(
                    center
                    + axis * halfWidth
                    + radial * outerRadius);

                mesh.Positions.Add(
                    center
                    + axis * halfWidth
                    + radial * innerRadius);
            }


            for (int i = 0;
                 i < segments;
                 i++)
            {
                int next =
                    (i + 1)
                    % segments;

                int a =
                    i * 4;

                int b =
                    next * 4;


                // Vorderseite
                mesh.TriangleIndices.Add(a);
                mesh.TriangleIndices.Add(b);
                mesh.TriangleIndices.Add(a + 1);

                mesh.TriangleIndices.Add(a + 1);
                mesh.TriangleIndices.Add(b);
                mesh.TriangleIndices.Add(b + 1);


                // Rückseite
                mesh.TriangleIndices.Add(a + 2);
                mesh.TriangleIndices.Add(a + 3);
                mesh.TriangleIndices.Add(b + 2);

                mesh.TriangleIndices.Add(a + 3);
                mesh.TriangleIndices.Add(b + 3);
                mesh.TriangleIndices.Add(b + 2);


                // Außenfläche
                mesh.TriangleIndices.Add(a);
                mesh.TriangleIndices.Add(a + 2);
                mesh.TriangleIndices.Add(b);

                mesh.TriangleIndices.Add(b);
                mesh.TriangleIndices.Add(a + 2);
                mesh.TriangleIndices.Add(b + 2);


                // Innenfläche / Bohrung
                mesh.TriangleIndices.Add(a + 1);
                mesh.TriangleIndices.Add(b + 1);
                mesh.TriangleIndices.Add(a + 3);

                mesh.TriangleIndices.Add(a + 3);
                mesh.TriangleIndices.Add(b + 1);
                mesh.TriangleIndices.Add(b + 3);
            }


            DiffuseMaterial material =
                new DiffuseMaterial(
                    brush);

            GeometryModel3D model =
                new GeometryModel3D
                {
                    Geometry = mesh,
                    Material = material,
                    BackMaterial = material
                };

            worldPartMap[model] =
                placed;

            WorldViewport.Children.Add(
                new ModelVisual3D
                {
                    Content = model
                });
        }
        private void AddArcTube(
    Point3D center,
    Vector3D axis,
    Vector3D side1,
    Vector3D side2,
    double arcRadius,
    double tubeRadius,
    double startAngleDegrees,
    double sweepAngleDegrees,
    PlacedPart placed,
    Brush brush)
        {
            const int arcSegments = 18;
            const int tubeSegments = 8;

            axis.Normalize();
            side1.Normalize();
            side2.Normalize();

            MeshGeometry3D mesh =
                new MeshGeometry3D();

            double start =
                startAngleDegrees
                * Math.PI / 180.0;

            double sweep =
                sweepAngleDegrees
                * Math.PI / 180.0;


            for (int i = 0;
                 i <= arcSegments;
                 i++)
            {
                double t =
                    (double)i
                    / arcSegments;

                double angle =
                    start
                    + sweep * t;

                Vector3D radial =
                    side1 * Math.Cos(angle)
                    + side2 * Math.Sin(angle);

                Point3D arcCenter =
                    center
                    + radial * arcRadius;


                for (int j = 0;
                     j < tubeSegments;
                     j++)
                {
                    double tubeAngle =
                        2.0 * Math.PI
                        * j
                        / tubeSegments;

                    Vector3D offset =
                        radial
                            * Math.Cos(tubeAngle)
                            * tubeRadius

                        + axis
                            * Math.Sin(tubeAngle)
                            * tubeRadius;

                    mesh.Positions.Add(
                        arcCenter + offset);
                }
            }


            for (int i = 0;
                 i < arcSegments;
                 i++)
            {
                for (int j = 0;
                     j < tubeSegments;
                     j++)
                {
                    int nextJ =
                        (j + 1)
                        % tubeSegments;

                    int a =
                        i * tubeSegments + j;

                    int b =
                        (i + 1)
                        * tubeSegments + j;

                    int c =
                        i * tubeSegments
                        + nextJ;

                    int d =
                        (i + 1)
                        * tubeSegments
                        + nextJ;

                    mesh.TriangleIndices.Add(a);
                    mesh.TriangleIndices.Add(b);
                    mesh.TriangleIndices.Add(c);

                    mesh.TriangleIndices.Add(c);
                    mesh.TriangleIndices.Add(b);
                    mesh.TriangleIndices.Add(d);
                }
            }


            DiffuseMaterial material =
                new DiffuseMaterial(
                    brush);

            GeometryModel3D model =
                new GeometryModel3D
                {
                    Geometry = mesh,
                    Material = material,
                    BackMaterial = material
                };

            worldPartMap[model] =
                placed;

            WorldViewport.Children.Add(
                new ModelVisual3D
                {
                    Content = model
                });
        }

        private void DrawBigWheel2D(
      PlacedPart placed,
      BigWheel wheel)
        {
            bool isCurrentLayer =
                Math.Abs(
                    placed.Transform.Position.Z
                    - currentPlanZ)
                < 0.001;

            Vector3 cellCenter =
                GetCellCenter(placed);

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

            Brush outlineBrush;
            Brush tireBrush;
            Brush rimBrush;

            if (isSelected)
            {
                outlineBrush =
                    Brushes.LimeGreen;

                tireBrush =
                    new SolidColorBrush(
                        Color.FromArgb(
                            180,
                            70,
                            70,
                            70));

                rimBrush =
                    HighlightBrush(
                        PaksyRed);
            }
            else if (isCurrentLayer)
            {
                outlineBrush =
                    Brushes.Black;

                tireBrush =
                    Brushes.Black;

                rimBrush =
                    PaksyRed;
            }
            else
            {
                outlineBrush =
                    new SolidColorBrush(
                        Color.FromArgb(
                            55,
                            0,
                            0,
                            0));

                tireBrush =
                    new SolidColorBrush(
                        Color.FromArgb(
                            45,
                            0,
                            0,
                            0));

                rimBrush =
                    new SolidColorBrush(
                        Color.FromArgb(
                            45,
                            235,
                            45,
                            45));
            }

            double wheelDiameter =
                wheel.OuterDiameter * Scale;

            double rimDiameter =
                wheel.RimDiameter * Scale;

            double tireWidth =
                wheel.TireWidth * Scale;

            double totalWidth =
                wheel.Width * Scale;

            double holeDiameter =
                wheel.HoleDiameter * Scale;

            double halfWidth =
                totalWidth / 2.0;

            double armEndDistance =
                Grider.CellSize
                * Scale
                / 2.0;

            double wheelCenterDistance =
                armEndDistance
                - halfWidth;

            Vector3 wheelCenter =
                new Vector3(
                    cellCenter.X,
                    cellCenter.Y,
                    cellCenter.Z);

            switch (wheelFace)
            {
                case Face.Right:
                    wheelCenter.X +=
                        wheelCenterDistance;
                    break;

                case Face.Left:
                    wheelCenter.X -=
                        wheelCenterDistance;
                    break;

                case Face.Top:
                    wheelCenter.Y -=
                        wheelCenterDistance;
                    break;

                case Face.Bottom:
                    wheelCenter.Y +=
                        wheelCenterDistance;
                    break;

                case Face.Front:
                case Face.Back:

                    DrawBigWheelFromFront(
     placed,
     wheel,
     wheelCenter,
     tireBrush,
     rimBrush,
     outlineBrush);
                    return;
            }

            bool horizontalAxis =
                wheelFace == Face.Left ||
                wheelFace == Face.Right;

            Rectangle tire =
                new Rectangle
                {
                    Fill =
                        tireBrush,

                    Stroke =
                        outlineBrush,

                    StrokeThickness =
                        isSelected
                            ? 2.0
                            : 1.0,

                    RadiusX =
                        tireWidth / 2.0,

                    RadiusY =
                        tireWidth / 2.0
                };

            if (horizontalAxis)
            {
                tire.Width =
                    tireWidth;

                tire.Height =
                    wheelDiameter;
            }
            else
            {
                tire.Width =
                    wheelDiameter;

                tire.Height =
                    tireWidth;
            }

            Canvas.SetLeft(
                tire,
                wheelCenter.X
                - tire.Width / 2.0);

            Canvas.SetTop(
                tire,
                wheelCenter.Y
                - tire.Height / 2.0);

            BuildArea.Children.Add(
                tire);

            double rimBodyWidth =
                wheel.RimBodyThickness
                * Scale;

            Rectangle rim =
                new Rectangle
                {
                    Fill =
                        rimBrush,

                    Stroke =
                        isCurrentLayer || isSelected
                            ? Brushes.DarkRed
                            : new SolidColorBrush(
                                Color.FromArgb(
                                    45,
                                    120,
                                    0,
                                    0)),

                    StrokeThickness =
                        1.0,

                    RadiusX =
                        2,

                    RadiusY =
                        2
                };

            if (horizontalAxis)
            {
                rim.Width =
                    rimBodyWidth;

                rim.Height =
                    rimDiameter;
            }
            else
            {
                rim.Width =
                    rimDiameter;

                rim.Height =
                    rimBodyWidth;
            }

            Canvas.SetLeft(
                rim,
                wheelCenter.X
                - rim.Width / 2.0);

            Canvas.SetTop(
                rim,
                wheelCenter.Y
                - rim.Height / 2.0);

            BuildArea.Children.Add(
                rim);

            Rectangle hub =
                new Rectangle
                {
                    Fill =
                        rimBrush,

                    Stroke =
                        isCurrentLayer || isSelected
                            ? Brushes.DarkRed
                            : new SolidColorBrush(
                                Color.FromArgb(
                                    45,
                                    120,
                                    0,
                                    0)),

                    StrokeThickness =
                        1.0,

                    RadiusX =
                        2,

                    RadiusY =
                        2
                };

            double hubLength =
                wheel.BoreDepth
                * Scale;

            if (horizontalAxis)
            {
                hub.Width =
                    hubLength;

                hub.Height =
                    holeDiameter;
            }
            else
            {
                hub.Width =
                    holeDiameter;

                hub.Height =
                    hubLength;
            }

            Canvas.SetLeft(
                hub,
                wheelCenter.X
                - hub.Width / 2.0);

            Canvas.SetTop(
                hub,
                wheelCenter.Y
                - hub.Height / 2.0);

            BuildArea.Children.Add(
                hub);
        }


        private void DrawBigWheelFromFront(
      PlacedPart placed,
      BigWheel wheel,
      Vector3 center,
      Brush tireBrush,
      Brush rimBrush,
      Brush outlineBrush)
        {
            bool isCurrentLayer =
                Math.Abs(
                    placed.Transform.Position.Z
                    - currentPlanZ)
                < 0.001;

            bool isSelected =
                selectedParts.Contains(placed);

            double outerDiameter =
                wheel.OuterDiameter
                * Scale;

            double rimDiameter =
                wheel.RimDiameter
                * Scale;

            double centerHoleDiameter =
                wheel.HoleDiameter
                * Scale;

            double sideHoleDiameter =
                wheel.SideHoleDiameter
                * Scale;

            double sideHoleRadius =
                wheel.SideHoleRadius
                * Scale;


            // ------------------------------------------------------------
            // LOCHFARBE
            // ------------------------------------------------------------

            Brush holeBrush;

            if (isSelected ||
                isCurrentLayer)
            {
                holeBrush =
                    Brushes.Black;
            }
            else
            {
                holeBrush =
                    new SolidColorBrush(
                        Color.FromArgb(
                            45,
                            0,
                            0,
                            0));
            }


            // ------------------------------------------------------------
            // REIFEN
            // ------------------------------------------------------------

            Ellipse tire =
                new Ellipse
                {
                    Width =
                        outerDiameter,

                    Height =
                        outerDiameter,

                    Fill =
                        tireBrush,

                    Stroke =
                        outlineBrush,

                    StrokeThickness =
                        isSelected
                            ? 2.0
                            : 1.0
                };

            Canvas.SetLeft(
                tire,
                center.X
                - outerDiameter / 2.0);

            Canvas.SetTop(
                tire,
                center.Y
                - outerDiameter / 2.0);

            BuildArea.Children.Add(
                tire);


            // ------------------------------------------------------------
            // FELGE
            // ------------------------------------------------------------

            Ellipse rim =
                new Ellipse
                {
                    Width =
                        rimDiameter,

                    Height =
                        rimDiameter,

                    Fill =
                        rimBrush,

                    Stroke =
                        isCurrentLayer || isSelected
                            ? Brushes.DarkRed
                            : new SolidColorBrush(
                                Color.FromArgb(
                                    45,
                                    120,
                                    0,
                                    0)),

                    StrokeThickness =
                        1.0
                };

            Canvas.SetLeft(
                rim,
                center.X
                - rimDiameter / 2.0);

            Canvas.SetTop(
                rim,
                center.Y
                - rimDiameter / 2.0);

            BuildArea.Children.Add(
                rim);


            // ------------------------------------------------------------
            // MITTELLOCH
            // ------------------------------------------------------------

            Ellipse centerHole =
                new Ellipse
                {
                    Width =
                        centerHoleDiameter,

                    Height =
                        centerHoleDiameter,

                    Fill =
                        holeBrush
                };

            Canvas.SetLeft(
                centerHole,
                center.X
                - centerHoleDiameter / 2.0);

            Canvas.SetTop(
                centerHole,
                center.Y
                - centerHoleDiameter / 2.0);

            BuildArea.Children.Add(
                centerHole);


            // ------------------------------------------------------------
            // VIER ÄUSSERE LÖCHER
            // ------------------------------------------------------------

            for (int i = 0;
                 i < wheel.SideHoleCount;
                 i++)
            {
                double angle =
                    Math.PI / 4.0
                    + i * Math.PI / 2.0;

                double x =
                    center.X
                    + Math.Cos(angle)
                    * sideHoleRadius;

                double y =
                    center.Y
                    + Math.Sin(angle)
                    * sideHoleRadius;

                Ellipse hole =
                    new Ellipse
                    {
                        Width =
                            sideHoleDiameter,

                        Height =
                            sideHoleDiameter,

                        Fill =
                            holeBrush
                    };

                Canvas.SetLeft(
                    hole,
                    x
                    - sideHoleDiameter / 2.0);

                Canvas.SetTop(
                    hole,
                    y
                    - sideHoleDiameter / 2.0);

                BuildArea.Children.Add(
                    hole);
            }
        }

        private void AddRecentFile(string filePath)
        {
            recentFiles.RemoveAll(
                path => string.Equals(
                    path,
                    filePath,
                    StringComparison.OrdinalIgnoreCase));

            recentFiles.Insert(0, filePath);

            while (recentFiles.Count > MaxRecentFiles)
            {
                recentFiles.RemoveAt(
                    recentFiles.Count - 1);
            }

            SaveRecentFiles();

            UpdateRecentFilesMenu();
        }

        private void SaveRecentFiles()
        {
            try
            {
                string directory =
                    System.IO.Path.GetDirectoryName(
                        RecentFilesPath);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json =
                    JsonSerializer.Serialize(
                        recentFiles,
                        new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });

                File.WriteAllText(
                    RecentFilesPath,
                    json);
            }
            catch
            {
                // Recent-Files sind Komfortfunktion.
                // Ein Fehler darf PlastiCAD nicht stoppen.
            }
        }
        private void UpdateRecentFilesMenu()
        {
            // ------------------------------------------------------------
            // ALTE RECENT-FILE-EINTRÄGE ENTFERNEN
            // ------------------------------------------------------------

            for (int i = FileMenu.Items.Count - 1; i >= 0; i--)
            {
                if (FileMenu.Items[i] is MenuItem item &&
                    item.Tag as string == "RecentFile")
                {
                    FileMenu.Items.RemoveAt(i);
                }
            }

            // ------------------------------------------------------------
            // POSITION DES SEPARATORS FINDEN
            //
            // Die Dateien werden direkt VOR diesem Separator eingefügt.
            // ------------------------------------------------------------

            int insertIndex =
                FileMenu.Items.IndexOf(
                    RecentFilesSeparator);

            if (insertIndex < 0)
                return;

            // ------------------------------------------------------------
            // KEINE DATEIEN
            // ------------------------------------------------------------

            if (recentFiles.Count == 0)
            {
                MenuItem emptyItem =
                    new MenuItem
                    {
                        Header = "(keine)",
                        IsEnabled = false,
                        Tag = "RecentFile"
                    };

                FileMenu.Items.Insert(
                    insertIndex,
                    emptyItem);

                return;
            }

            // ------------------------------------------------------------
            // LETZTE DATEIEN DIREKT EINTRAGEN
            // ------------------------------------------------------------

            foreach (string filePath in recentFiles)
            {
                MenuItem item =
                    new MenuItem
                    {
                        Header =
                            System.IO.Path.GetFileNameWithoutExtension(filePath),

                        ToolTip =
                            filePath,

                        Tag =
                            "RecentFile"
                    };

                // Dateipfad separat merken
                item.DataContext =
                    filePath;

                item.Click +=
                    RecentFileMenuItem_Click;

                FileMenu.Items.Insert(
                    insertIndex,
                    item);

                insertIndex++;
            }
        }

        private void RecentFileMenuItem_Click(
     object sender,
     RoutedEventArgs e)
        {
            if (sender is MenuItem item &&
                item.DataContext is string filePath)
            {
                LoadProjectFromFile(filePath);
            }
        }
        private void LoadRecentFiles()
        {
            recentFiles.Clear();

            try
            {
                if (!File.Exists(RecentFilesPath))
                {
                    UpdateRecentFilesMenu();
                    return;
                }

                string json =
                    File.ReadAllText(
                        RecentFilesPath);

                List<string> savedFiles =
                    JsonSerializer.Deserialize<List<string>>(
                        json);

                if (savedFiles != null)
                {
                    foreach (string filePath in savedFiles)
                    {
                        if (File.Exists(filePath))
                        {
                            recentFiles.Add(filePath);
                        }

                        if (recentFiles.Count >= MaxRecentFiles)
                            break;
                    }
                }
            }
            catch
            {
                recentFiles.Clear();
            }

            UpdateRecentFilesMenu();
        }

        private void LoadProjectFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            if (!File.Exists(filePath))
            {
                StatusText.Text = "Datei wurde nicht gefunden";
                return;
            }

            currentProjectFileName = filePath;

            UpdateWindowTitle();

            string json = File.ReadAllText(filePath);

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
            AddRecentFile(filePath);
            StatusText.Text =
                $"{assembly.PlacedParts.Count} Bauteil(e) geladen";

            worldCameraInitialized = false;

            RedrawScene();
        }


        private void ElbowPreview_MouseEnter(
    object sender,
    MouseEventArgs e)
        {
            if (elbowPreviewTimer != null)
                return;

            elbowPreviewTimer =
                new DispatcherTimer
                {
                    Interval =
                        TimeSpan.FromMilliseconds(20)
                };

            elbowPreviewTimer.Tick +=
                ElbowPreviewTimer_Tick;

            elbowPreviewTimer.Start();
        }

        private void ElbowPreview_MouseLeave(
    object sender,
    MouseEventArgs e)
        {
            if (elbowPreviewTimer != null)
            {
                elbowPreviewTimer.Stop();

                elbowPreviewTimer.Tick -=
                    ElbowPreviewTimer_Tick;

                elbowPreviewTimer = null;
            }

            elbowPreviewAngle = 0;

            ElbowPreviewModel.Transform =
                Transform3D.Identity;
        }

        private void ElbowPreviewTimer_Tick(
    object sender,
    EventArgs e)
        {
            elbowPreviewAngle += 2.0;

            if (elbowPreviewAngle >= 360.0)
            {
                elbowPreviewAngle -= 360.0;
            }

            AxisAngleRotation3D rotation =
                new AxisAngleRotation3D(
                    new Vector3D(
                        0,
                        1,
                        0),
                    elbowPreviewAngle);

            ElbowPreviewModel.Transform =
                new RotateTransform3D(
                    rotation,
                    new Point3D(
                        0,
                        0,
                        0));
        }


        private void ToolboxPreview_MouseEnter(
    object sender,
    MouseEventArgs e)
        {
            if (!(sender is Button button))
                return;

            if (!(button.Tag is string partName))
                return;

            activeToolboxPreviewPartName =
                partName;

            activeToolboxPreviewModel =
                GetToolboxPreviewModel(
                    partName);

            if (activeToolboxPreviewModel == null)
                return;

            toolboxPreviewAngle = 0.0;

            if (toolboxPreviewTimer == null)
            {
                toolboxPreviewTimer =
                    new DispatcherTimer
                    {
                        Interval =
                            TimeSpan.FromMilliseconds(20)
                    };

                toolboxPreviewTimer.Tick +=
                    ToolboxPreviewTimer_Tick;
            }

            toolboxPreviewTimer.Start();
        }

        private void ToolboxPreview_MouseLeave(
    object sender,
    MouseEventArgs e)
        {
            if (toolboxPreviewTimer != null)
            {
                toolboxPreviewTimer.Stop();
            }

            if (activeToolboxPreviewModel != null &&
                !string.IsNullOrWhiteSpace(
                    activeToolboxPreviewPartName))
            {
                ApplyToolboxPreviewStartRotation(
                    activeToolboxPreviewPartName,
                    activeToolboxPreviewModel);
            }

            activeToolboxPreviewModel = null;
            activeToolboxPreviewPartName = null;

            toolboxPreviewAngle = 0.0;
        }

        private void ToolboxPreviewTimer_Tick(
     object sender,
     EventArgs e)
        {
            if (activeToolboxPreviewModel == null ||
                string.IsNullOrWhiteSpace(
                    activeToolboxPreviewPartName))
            {
                return;
            }

            toolboxPreviewAngle += 2.0;

            if (toolboxPreviewAngle >= 360.0)
            {
                toolboxPreviewAngle -= 360.0;
            }

            Vector3D startRotation =
                GetToolboxPreviewStartRotation(
                    activeToolboxPreviewPartName);

            Transform3DGroup transforms =
                new Transform3DGroup();

            // Startwinkel X
            transforms.Children.Add(
                new RotateTransform3D(
                    new AxisAngleRotation3D(
                        new Vector3D(1, 0, 0),
                        startRotation.X)));

            // Startwinkel Y + Hover-Drehung
            transforms.Children.Add(
                new RotateTransform3D(
                    new AxisAngleRotation3D(
                        new Vector3D(0, 1, 0),
                        startRotation.Y
                        + toolboxPreviewAngle)));

            // Startwinkel Z
            transforms.Children.Add(
                new RotateTransform3D(
                    new AxisAngleRotation3D(
                        new Vector3D(0, 0, 1),
                        startRotation.Z)));

            activeToolboxPreviewModel.Transform =
                transforms;
        }

        private void CreateToolboxPreviews()
        {
            CreateStructuralToolboxPreview(
                "Rohr 27,5 mm",
                PipePreviewModel);

            CreateStructuralToolboxPreview(
                "90° Winkel",
                ElbowPreviewModel);

            CreateStructuralToolboxPreview(
                "T-Stück",
                TeePreviewModel);

            CreateStructuralToolboxPreview(
                "Kreuz",
                CrossPreviewModel);

            CreateStructuralToolboxPreview(
                "Corner",
                CornerPreviewModel);

            CreateStructuralToolboxPreview(
                "Edge",
                EdgePreviewModel);

            CreateStructuralToolboxPreview(
                "Stand",
                StandPreviewModel);

            CreateStructuralToolboxPreview(
                "SpaceCross",
                SpaceCrossPreviewModel);

            // Spezialteile

            CreateCubeToolboxPreview(
                CubePreviewModel);

            CreateBallToolboxPreview(
                BallPreviewModel);

            CreatePlateToolboxPreview(
                PlatePreviewModel);

            CreateBigPlateToolboxPreview(
                BigPlatePreviewModel);

            CreateWindowToolboxPreview(
                WindowPreviewModel);

            CreateHolePlateToolboxPreview
                (HolePlatePreviewModel);

            CreateSlatPlateToolboxPreview
                 (SlatPlatePreviewModel);

            CreateWheelToolboxPreview(
                WheelPreviewModel);

            CreateBigWheelToolboxPreview(
                BigWheelPreviewModel);

            CreateEndCapToolboxPreview(
    EndCapPreviewModel);

        }
        private void CreateHolePlateToolboxPreview(Model3DGroup previewModel)
        {
            previewModel.Children.Clear();

            if (!(PartLibrary.Parts.FirstOrDefault(p => p.Name == "Lochplatte") is HolePlate plate))
                return;

            double s = 0.80;
            GeometryModel3D model = CreateRectangularPlateWithHole(
                new Point3D(0, 0, 0),
                new Vector3D(0, 1, 0),
                new Vector3D(1, 0, 0),
                plate.Width / 100.0 * s,
                plate.Height / 100.0 * s,
                plate.Thickness / 100.0 * s,
                (plate.HoleDiameter / 2.0) / 100.0 * s,
                PaksyRed);

            if (model != null)
                previewModel.Children.Add(model);

            ApplyToolboxPreviewStartRotation("Lochplatte", previewModel);
        }
        private void CreateEndCapToolboxPreview(
    Model3DGroup previewModel)
        {
            previewModel.Children.Clear();

            Part part =
                PartLibrary.Parts.FirstOrDefault(
                    p => p.Name == "Endkappe");

            if (!(part is EndCap endCap))
                return;

            double previewScale =
                2.40;

            Point3D center =
                new Point3D(
                    0,
                    0,
                    0);

            Vector3D axis =
                new Vector3D(
                    1,
                    0,
                    0);

            axis.Normalize();

            Brush capBrush =
                Brushes.Gold;


            // ------------------------------------------------------------
            // GLEICHE MASSE WIE AddEndCap() IN PAKSY WORLD
            // ------------------------------------------------------------

            double flangeRadius =
                0.060
                * previewScale;

            double flangeLength =
                0.010
                * previewScale;

            double coneRadius =
                flangeRadius;

            double coneLength =
                0.035
                * previewScale;


            // ------------------------------------------------------------
            // FLANSCH
            // ------------------------------------------------------------

            Point3D flangeStart =
                center
                - axis * (flangeLength / 2.0);

            Point3D flangeEnd =
                center
                + axis * (flangeLength / 2.0);

            GeometryModel3D flange =
                CreatePreviewCylinder(
                    flangeStart,
                    flangeEnd,
                    flangeRadius,
                    capBrush);

            if (flange != null)
            {
                previewModel.Children.Add(
                    flange);
            }


            // ------------------------------------------------------------
            // KEGEL
            // ------------------------------------------------------------

            Point3D coneStart =
                flangeEnd;

            Point3D coneTip =
                coneStart
                + axis * coneLength;

            GeometryModel3D cone =
                CreatePreviewCone(
                    coneStart,
                    coneTip,
                    coneRadius,
                    capBrush);

            if (cone != null)
            {
                previewModel.Children.Add(
                    cone);
            }


            ApplyToolboxPreviewStartRotation(
                "Endkappe",
                previewModel);
        }
        private GeometryModel3D CreatePreviewCone(
    Point3D start,
    Point3D end,
    double radius,
    Brush brush)
        {
            const int segments = 32;

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

            side1.Normalize();

            Vector3D side2 =
                Vector3D.CrossProduct(
                    axis,
                    side1);

            side2.Normalize();


            MeshGeometry3D mesh =
                new MeshGeometry3D();

            // Spitze
            mesh.Positions.Add(
                end);

            // Mittelpunkt der Grundfläche
            mesh.Positions.Add(
                start);


            // Kreis
            for (int i = 0;
                 i < segments;
                 i++)
            {
                double angle =
                    2.0
                    * Math.PI
                    * i
                    / segments;

                Vector3D offset =
                    side1
                        * Math.Cos(angle)
                        * radius
                    +
                    side2
                        * Math.Sin(angle)
                        * radius;

                mesh.Positions.Add(
                    start + offset);
            }


            for (int i = 0;
                 i < segments;
                 i++)
            {
                int current =
                    2 + i;

                int next =
                    2 + ((i + 1) % segments);


                // Mantelfläche
                mesh.TriangleIndices.Add(0);
                mesh.TriangleIndices.Add(current);
                mesh.TriangleIndices.Add(next);


                // Grundfläche
                mesh.TriangleIndices.Add(1);
                mesh.TriangleIndices.Add(next);
                mesh.TriangleIndices.Add(current);
            }


            DiffuseMaterial material =
                new DiffuseMaterial(
                    brush);

            return new GeometryModel3D
            {
                Geometry = mesh,
                Material = material,
                BackMaterial = material
            };
        }
        private void CreateWindowToolboxPreview(
    Model3DGroup previewModel)
        {
            previewModel.Children.Clear();

            Part part =
                PartLibrary.Parts.FirstOrDefault(
                    p => p.Name == "Fenster");

            if (!(part is WindowPlate windowPlate))
                return;

            double previewScale =
                0.80;

            double width =
                windowPlate.Width / 100.0
                * previewScale;

            double height =
                windowPlate.Height / 100.0
                * previewScale;

            double thickness =
                windowPlate.Thickness / 100.0
                * previewScale;

            double barWidth =
                windowPlate.CenterBarWidth / 100.0
                * previewScale;

            double barThickness =
                thickness + 0.002;


            // Transparente Scheibe
            Brush glassBrush =
                new SolidColorBrush(
                    Color.FromArgb(
                        70,
                        180,
                        230,
                        255));


            // Mittelsteg
            Brush centerBarBrush =
                new SolidColorBrush(
                    Color.FromArgb(
                        150,
                        190,
                        210,
                        220));


            Point3D center =
                new Point3D(
                    0,
                    0,
                    0);


            // ------------------------------------------------------------
            // GLASSCHEIBE
            // ------------------------------------------------------------

            GeometryModel3D glass =
                CreatePreviewBox(
                    center,
                    width,
                    thickness,
                    height,
                    glassBrush);

            previewModel.Children.Add(
                glass);


            // ------------------------------------------------------------
            // MITTELSTEG
            // ------------------------------------------------------------

            GeometryModel3D centerBar =
                CreatePreviewBox(
                    center,
                    barWidth,
                    barThickness,
                    height,
                    centerBarBrush);

            previewModel.Children.Add(
                centerBar);


            ApplyToolboxPreviewStartRotation(
                "Fenster",
                previewModel);
        }
        private void CreatePlateToolboxPreview(
    Model3DGroup previewModel)
        {
            previewModel.Children.Clear();

            Part part =
                PartLibrary.Parts.FirstOrDefault(
                    p => p.Name == "Platte");

            if (!(part is Plate plate))
                return;

            Brush brush =
                PaksyRed;

            double previewScale = 0.80;

            double width =
                plate.Width / 100.0
                * previewScale;

            double height =
                plate.Height / 100.0
                * previewScale;

            double thickness =
                plate.Thickness / 100.0
                * previewScale;

            GeometryModel3D model =
                CreatePreviewBox(
                    new Point3D(0, 0, 0),
                    width,
                    thickness,
                    height,
                    brush);

            previewModel.Children.Add(
                model);

            ApplyToolboxPreviewStartRotation(
                "Platte",
                previewModel);
        }

        private void CreateBigPlateToolboxPreview(
    Model3DGroup previewModel)
        {
            previewModel.Children.Clear();

            Part part =
                PartLibrary.Parts.FirstOrDefault(
                    p => p.Name == "Große Platte");

            if (!(part is BigPlate plate))
                return;

            Brush brush =
                PaksyRed;
            double previewScale = 0.80;

            double outerSize =
     plate.OuterSize / 100.0
     * previewScale;

            double innerSize =
                plate.InnerSize / 100.0
                * previewScale;

            double plateThickness =
                plate.PlateThickness / 100.0
                * previewScale;

            double ribLength =
                plate.RibLength / 100.0
                * previewScale;

            double ribHeight =
                plate.RibHeight / 100.0
                * previewScale;

            double ribThickness =
                plate.RibThickness / 100.0
                * previewScale;

            double ribOffset =
                (
                    plate.RibClearDistance / 2.0
                    + plate.RibThickness / 2.0
                ) / 100.0
                * previewScale;

            double plateCenterOffset =
                (
                    plate.TotalThickness / 2.0
                    - plate.PlateThickness / 2.0
                ) / 100.0
                * previewScale;

            // Große Vorderseite
            previewModel.Children.Add(
                CreatePreviewBox(
                    new Point3D(
                        0,
                        plateCenterOffset,
                        0),

                    outerSize,
                    plateThickness,
                    outerSize,
                    brush));


            // Kleine Rückseite
            previewModel.Children.Add(
                CreatePreviewBox(
                    new Point3D(
                        0,
                        -plateCenterOffset,
                        0),

                    innerSize,
                    plateThickness,
                    innerSize,
                    brush));


            // Erster Steg
            previewModel.Children.Add(
                CreatePreviewBox(
                    new Point3D(
                        -ribOffset,
                        0,
                        0),

                    ribThickness,
                    ribHeight,
                    ribLength,
                    brush));


            // Zweiter Steg
            previewModel.Children.Add(
                CreatePreviewBox(
                    new Point3D(
                        ribOffset,
                        0,
                        0),

                    ribThickness,
                    ribHeight,
                    ribLength,
                    brush));


            ApplyToolboxPreviewStartRotation(
                "Große Platte",
                previewModel);
        }
        private Model3DGroup GetToolboxPreviewModel(
     string partName)
        {
            switch (partName)
            {
                case "Rohr 27,5 mm":
                    return PipePreviewModel;

                case "90° Winkel":
                    return ElbowPreviewModel;

                case "T-Stück":
                    return TeePreviewModel;

                case "Kreuz":
                    return CrossPreviewModel;

                case "Corner":
                    return CornerPreviewModel;

                case "Edge":
                    return EdgePreviewModel;

                case "Stand":
                    return StandPreviewModel;

                case "SpaceCross":
                    return SpaceCrossPreviewModel;

                case "Würfel":
                    return CubePreviewModel;

                case "Kugel":
                    return BallPreviewModel;

                case "Platte":
                    return PlatePreviewModel;

                case "Große Platte":
                    return BigPlatePreviewModel;
                case "Fenster":
                    return WindowPreviewModel;

                case "Lochplatte":
                    return HolePlatePreviewModel;

                case "Streifenplatte":
                    return SlatPlatePreviewModel;

                case "Rad":
                    return WheelPreviewModel;

                case "Big Rad":
                    return BigWheelPreviewModel;

                case "Endkappe":
                    return EndCapPreviewModel;
                default:
                    return null;
            }
        }

        private void CreateSlatPlateToolboxPreview(Model3DGroup previewModel)
        {
            previewModel.Children.Clear();

            if (!(PartLibrary.Parts.FirstOrDefault(p => p.Name == "Streifenplatte") is SlatPlate plate))
                return;

            double s = 0.9;
            double w = plate.Width / 100.0 * s;
            double t = Math.Max(plate.Thickness / 100.0 * s, 0.008);
            double radius = (plate.GutterDiameter / 2.0) / 100.0 * s;
            double total =
                (plate.OuterSlatWidth * 2
                 + plate.InnerSlatWidth * 2
                 + plate.GapWidth * 3) / 100.0 * s;

            Vector3D across = new Vector3D(1, 0, 0);
            Vector3D stack = new Vector3D(0, 0, 1);
            Vector3D thick = new Vector3D(0, 1, 0);

            double cursor = -total / 2.0;
            foreach (double slatMm in plate.GetSlatWidths())
            {
                double slat = slatMm / 100.0 * s;
                Point3D center = new Point3D(0, 0, cursor + slat / 2.0);

                previewModel.Children.Add(
                    CreatePreviewBox(center, w, t, slat, PaksyYellow));

                cursor += slat + plate.GapWidth / 100.0 * s;
            }

            double axisOffset = w / 2.0 + radius;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3D outward = across * side;
                Point3D gutterCenter = new Point3D(outward.X * axisOffset, 0, 0);

                GeometryModel3D gutter = CreateQuarterCylinder(
                    gutterCenter,
                    stack,
                    outward,
                    thick,
                    radius,
                    total,
                    PaksyYellow);

                if (gutter != null)
                    previewModel.Children.Add(gutter);
            }

            ApplyToolboxPreviewStartRotation("Streifenplatte", previewModel);
        }
        private Vector3D GetToolboxPreviewStartRotation(
    string partName)
        {
            switch (partName)
            {
                case "Rohr 27,5 mm":
                    return new Vector3D(0, 0, 0);

                case "90° Winkel":
                    return new Vector3D(0, 90, 0);

                case "T-Stück":
                    return new Vector3D(0, 0, 0);

                case "Kreuz":
                    return new Vector3D(0, 0, 0);

                case "Corner":
                    return new Vector3D(0, 90, 0);

                case "Edge":
                    return new Vector3D(10, 0, 30);

                case "Stand":
                    return new Vector3D(0, 0, 0);

                case "SpaceCross":
                    return new Vector3D(0, 0, 0);
                case "Platte":
                    return new Vector3D(30, 20, 30);

                case "Große Platte":
                    return new Vector3D(30, 20, 30);
                case "Fenster":
                    return new Vector3D(30, 20, 30);
                case "Lochplatte":
                    return new Vector3D(30, 20, 30);
                case "Streifenplatte":
                    return new Vector3D(30, 20, 30);
                case "Rad":
                    return new Vector3D(0, 0, 0);

                case "Big Rad":
                    return new Vector3D(0, 0, 0);
                case "Endkappe":
                    return new Vector3D(0, 0, 0);

                default:
                    return new Vector3D(0, 0, 0);
            }
        }


        private void ApplyToolboxPreviewStartRotation(
    string partName,
    Model3DGroup previewModel)
        {
            Vector3D rotation =
                GetToolboxPreviewStartRotation(
                    partName);

            Transform3DGroup transforms =
                new Transform3DGroup();

            transforms.Children.Add(
                new RotateTransform3D(
                    new AxisAngleRotation3D(
                        new Vector3D(1, 0, 0),
                        rotation.X)));

            transforms.Children.Add(
                new RotateTransform3D(
                    new AxisAngleRotation3D(
                        new Vector3D(0, 1, 0),
                        rotation.Y)));

            transforms.Children.Add(
                new RotateTransform3D(
                    new AxisAngleRotation3D(
                        new Vector3D(0, 0, 1),
                        rotation.Z)));

            previewModel.Transform =
                transforms;
        }

        private void CreateCubeToolboxPreview(
    Model3DGroup previewModel)
        {
            previewModel.Children.Clear();

            Part part =
                PartLibrary.Parts.FirstOrDefault(
                    p => p.Name == "Würfel");

            if (!(part is Cube cube))
                return;

            Point3D center =
                new Point3D(0, 0, 0);


            // Größe nur für die Darstellung in der Toolbox
            double previewScale = 0.70;

            double size =
                cube.Size / 100.0
                * previewScale;

            double cornerRadius =
                cube.CornerRadius / 100.0
                * previewScale;

            double holeRadius =
                cube.HoleDiameter / 200.0
                * previewScale;


            Brush blueBrush =
                new SolidColorBrush(
                    Color.FromRgb(
                        35,
                        140,
                        195));

            // ... Rest deiner Methode bleibt unverändert
            Brush holeEdgeBrush =
                new SolidColorBrush(
                    Color.FromRgb(
                        20,
                        35,
                        45));

            Brush holeInsideBrush =
                Brushes.Black;


            // ------------------------------------------------------------
            // ABGERUNDETER WÜRFEL
            // ------------------------------------------------------------

            double innerSize =
                size - 2.0 * cornerRadius;

            double halfInner =
                innerSize / 2.0;


            // Drei Grundkörper

            previewModel.Children.Add(
                CreatePreviewBox(
                    center,
                    size,
                    innerSize,
                    innerSize,
                    blueBrush));

            previewModel.Children.Add(
                CreatePreviewBox(
                    center,
                    innerSize,
                    size,
                    innerSize,
                    blueBrush));

            previewModel.Children.Add(
                CreatePreviewBox(
                    center,
                    innerSize,
                    innerSize,
                    size,
                    blueBrush));


            // ------------------------------------------------------------
            // KANTEN X
            // ------------------------------------------------------------

            foreach (double ySign in new[] { -1.0, 1.0 })
            {
                foreach (double zSign in new[] { -1.0, 1.0 })
                {
                    GeometryModel3D cylinder =
                        CreatePreviewCylinder(
                            new Point3D(
                                -halfInner,
                                ySign * halfInner,
                                zSign * halfInner),

                            new Point3D(
                                halfInner,
                                ySign * halfInner,
                                zSign * halfInner),

                            cornerRadius,
                            blueBrush);

                    if (cylinder != null)
                        previewModel.Children.Add(cylinder);
                }
            }


            // ------------------------------------------------------------
            // KANTEN Y
            // ------------------------------------------------------------

            foreach (double xSign in new[] { -1.0, 1.0 })
            {
                foreach (double zSign in new[] { -1.0, 1.0 })
                {
                    GeometryModel3D cylinder =
                        CreatePreviewCylinder(
                            new Point3D(
                                xSign * halfInner,
                                -halfInner,
                                zSign * halfInner),

                            new Point3D(
                                xSign * halfInner,
                                halfInner,
                                zSign * halfInner),

                            cornerRadius,
                            blueBrush);

                    if (cylinder != null)
                        previewModel.Children.Add(cylinder);
                }
            }


            // ------------------------------------------------------------
            // KANTEN Z
            // ------------------------------------------------------------

            foreach (double xSign in new[] { -1.0, 1.0 })
            {
                foreach (double ySign in new[] { -1.0, 1.0 })
                {
                    GeometryModel3D cylinder =
                        CreatePreviewCylinder(
                            new Point3D(
                                xSign * halfInner,
                                ySign * halfInner,
                                -halfInner),

                            new Point3D(
                                xSign * halfInner,
                                ySign * halfInner,
                                halfInner),

                            cornerRadius,
                            blueBrush);

                    if (cylinder != null)
                        previewModel.Children.Add(cylinder);
                }
            }


            // ------------------------------------------------------------
            // 8 RUNDE ECKEN
            // ------------------------------------------------------------

            foreach (double xSign in new[] { -1.0, 1.0 })
            {
                foreach (double ySign in new[] { -1.0, 1.0 })
                {
                    foreach (double zSign in new[] { -1.0, 1.0 })
                    {
                        previewModel.Children.Add(
                            CreatePreviewSphere(
                                new Point3D(
                                    xSign * halfInner,
                                    ySign * halfInner,
                                    zSign * halfInner),

                                cornerRadius,
                                blueBrush));
                    }
                }
            }


            // ------------------------------------------------------------
            // 6 BOHRUNGEN
            // ------------------------------------------------------------

            Vector3D[] directions =
            {
        new Vector3D(-1, 0, 0),
        new Vector3D( 1, 0, 0),
        new Vector3D(0, -1, 0),
        new Vector3D(0,  1, 0),
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0,  1)
    };

            double halfSize =
                size / 2.0;

            foreach (Vector3D direction in directions)
            {
                Vector3D normal =
                    direction;

                normal.Normalize();

                Point3D holeCenter =
                    center
                    + normal * (halfSize + 0.0003);

                previewModel.Children.Add(
                    CreatePreviewDisc(
                        holeCenter,
                        normal,
                        holeRadius,
                        holeEdgeBrush));

                previewModel.Children.Add(
                    CreatePreviewDisc(
                        holeCenter + normal * 0.0002,
                        normal,
                        holeRadius * 0.72,
                        holeInsideBrush));
            }
            ScaleTransform3D scale =
    new ScaleTransform3D(
        0.70,
        0.70,
        0.70);

            previewModel.Transform =
                scale;
            ApplyToolboxPreviewStartRotation(
                "Würfel",
                previewModel);
        }

        private void CreateBallToolboxPreview(
    Model3DGroup previewModel)
        {
            previewModel.Children.Clear();

            Part part =
                PartLibrary.Parts.FirstOrDefault(
                    p => p.Name == "Kugel");

            if (!(part is BallConnector ball))
                return;

            Point3D center =
                new Point3D(0, 0, 0);

            double ballRadius =
                ball.Diameter / 200.0;

            double holeRadius =
                ball.HoleDiameter / 200.0;

            Brush blueBrush =
                new SolidColorBrush(
                    Color.FromRgb(
                        35,
                        140,
                        195));

            Brush holeEdgeBrush =
                new SolidColorBrush(
                    Color.FromRgb(
                        20,
                        40,
                        50));

            Brush holeInsideBrush =
                Brushes.Black;


            // Kugel
            previewModel.Children.Add(
                CreatePreviewSphere(
                    center,
                    ballRadius,
                    blueBrush));


            // Sechs Sacklochöffnungen
            Vector3D[] directions =
            {
        new Vector3D(-1, 0, 0),
        new Vector3D( 1, 0, 0),
        new Vector3D(0, -1, 0),
        new Vector3D(0,  1, 0),
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0,  1)
    };

            foreach (Vector3D direction in directions)
            {
                Vector3D normal =
                    direction;

                normal.Normalize();

                Point3D holeCenter =
                    center
                    + normal
                    * (ballRadius + 0.0003);

                previewModel.Children.Add(
                    CreatePreviewDisc(
                        holeCenter,
                        normal,
                        holeRadius,
                        holeEdgeBrush));

                previewModel.Children.Add(
                    CreatePreviewDisc(
                        holeCenter
                        + normal * 0.0002,

                        normal,
                        holeRadius * 0.72,
                        holeInsideBrush));
            }

            ApplyToolboxPreviewStartRotation(
                "Kugel",
                previewModel);
        }

        private GeometryModel3D CreatePreviewBox(
    Point3D center,
    double width,
    double height,
    double depth,
    Brush brush)
        {
            double hx =
                width / 2.0;

            double hy =
                height / 2.0;

            double hz =
                depth / 2.0;

            MeshGeometry3D mesh =
                new MeshGeometry3D();

            mesh.Positions.Add(
                new Point3D(
                    center.X - hx,
                    center.Y - hy,
                    center.Z - hz));

            mesh.Positions.Add(
                new Point3D(
                    center.X + hx,
                    center.Y - hy,
                    center.Z - hz));

            mesh.Positions.Add(
                new Point3D(
                    center.X + hx,
                    center.Y + hy,
                    center.Z - hz));

            mesh.Positions.Add(
                new Point3D(
                    center.X - hx,
                    center.Y + hy,
                    center.Z - hz));

            mesh.Positions.Add(
                new Point3D(
                    center.X - hx,
                    center.Y - hy,
                    center.Z + hz));

            mesh.Positions.Add(
                new Point3D(
                    center.X + hx,
                    center.Y - hy,
                    center.Z + hz));

            mesh.Positions.Add(
                new Point3D(
                    center.X + hx,
                    center.Y + hy,
                    center.Z + hz));

            mesh.Positions.Add(
                new Point3D(
                    center.X - hx,
                    center.Y + hy,
                    center.Z + hz));

            int[] triangles =
            {
        0,1,2, 0,2,3,
        4,6,5, 4,7,6,
        0,4,5, 0,5,1,
        1,5,6, 1,6,2,
        2,6,7, 2,7,3,
        3,7,4, 3,4,0
    };

            foreach (int index in triangles)
            {
                mesh.TriangleIndices.Add(index);
            }

            DiffuseMaterial material =
                new DiffuseMaterial(
                    brush);

            return new GeometryModel3D
            {
                Geometry = mesh,
                Material = material,
                BackMaterial = material
            };
        }

        private GeometryModel3D CreatePreviewDisc(
    Point3D center,
    Vector3D normal,
    double radius,
    Brush brush)
        {
            const int segments = 32;

            if (normal.Length == 0)
                return null;

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

                mesh.TriangleIndices.Add(0);
                mesh.TriangleIndices.Add(index + 1);
                mesh.TriangleIndices.Add(next + 1);
            }

            DiffuseMaterial material =
                new DiffuseMaterial(
                    brush);

            return new GeometryModel3D
            {
                Geometry = mesh,
                Material = material,
                BackMaterial = material
            };
        }


        private void CreateWheelToolboxPreview(
    Model3DGroup previewModel)
        {
            previewModel.Children.Clear();

            Part part =
                PartLibrary.Parts.FirstOrDefault(
                    p => p.Name == "Rad");

            if (!(part is Wheel wheel))
                return;

            double previewScale = 0.80;

            Point3D center =
                new Point3D(0, 0, 0);

            // Radachse entlang X
            Vector3D axis =
                new Vector3D(1, 0, 0);

            double tubeRadius =
                wheel.TireThickness
                / 2.0
                / 100.0
                * previewScale;

            double majorRadius =
                (wheel.OuterDiameter
                - wheel.TireThickness)
                / 2.0
                / 100.0
                * previewScale;

            double rimOuterRadius =
                wheel.RimDiameter
                / 200.0
                * previewScale;

            double rimHoleRadius =
                wheel.HoleDiameter
                / 200.0
                * previewScale;

            double rimHalfWidth =
                wheel.Width
                * 0.42
                / 100.0
                * previewScale;


            // ------------------------------------------------------------
            // SCHWARZER REIFEN
            // ------------------------------------------------------------

            GeometryModel3D tire =
                CreatePreviewTorus(
                    center,
                    axis,
                    majorRadius,
                    tubeRadius,
                    Brushes.Black);

            if (tire != null)
            {
                previewModel.Children.Add(
                    tire);
            }


            // ------------------------------------------------------------
            // ROTE FELGE
            // ------------------------------------------------------------

            GeometryModel3D rim =
                CreatePreviewRim(
                    center,
                    axis,
                    rimOuterRadius,
                    rimHoleRadius,
                    rimHalfWidth,
                    PaksyRed);

            if (rim != null)
            {
                previewModel.Children.Add(
                    rim);
            }


            ApplyToolboxPreviewStartRotation(
                "Rad",
                previewModel);
        }

        private GeometryModel3D CreatePreviewTorus(
    Point3D center,
    Vector3D axis,
    double majorRadius,
    double tubeRadius,
    Brush brush)
        {
            const int majorSegments = 32;
            const int tubeSegments = 16;

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
                    side1 * Math.Cos(majorAngle)
                    + side2 * Math.Sin(majorAngle);

                Point3D ringCenter =
                    center
                    + radialDirection
                    * majorRadius;

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
                        * (Math.Cos(tubeAngle)
                        * tubeRadius)

                        + axis
                        * (Math.Sin(tubeAngle)
                        * tubeRadius);

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
                        majorIndex
                        * tubeSegments
                        + tubeIndex;

                    int b =
                        nextMajor
                        * tubeSegments
                        + tubeIndex;

                    int c =
                        majorIndex
                        * tubeSegments
                        + nextTube;

                    int d =
                        nextMajor
                        * tubeSegments
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
                new DiffuseMaterial(
                    brush);

            return new GeometryModel3D
            {
                Geometry = mesh,
                Material = material,
                BackMaterial = material
            };
        }

        private GeometryModel3D CreatePreviewRim(
    Point3D center,
    Vector3D axis,
    double outerRadius,
    double holeRadius,
    double halfWidth,
    Brush brush)
        {
            const int segments = 48;

            if (axis.Length == 0)
                return null;

            if (holeRadius <= 0 ||
                outerRadius <= holeRadius ||
                halfWidth <= 0)
            {
                return null;
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

            Point[] profile =
            {
        new Point(
            -halfWidth * 0.55,
            holeRadius),

        new Point(
            -halfWidth,
            holeRadius * 1.30),

        new Point(
            -halfWidth * 0.75,
            outerRadius * 0.72),

        new Point(
            -halfWidth * 0.55,
            outerRadius),

        new Point(
            halfWidth * 0.55,
            outerRadius),

        new Point(
            halfWidth * 0.75,
            outerRadius * 0.72),

        new Point(
            halfWidth,
            holeRadius * 1.30),

        new Point(
            halfWidth * 0.55,
            holeRadius)
    };

            MeshGeometry3D mesh =
                new MeshGeometry3D();

            int profileCount =
                profile.Length;

            for (int segment = 0;
                 segment < segments;
                 segment++)
            {
                double angle =
                    2.0 * Math.PI
                    * segment
                    / segments;

                Vector3D radialDirection =
                    side1 * Math.Cos(angle)
                    + side2 * Math.Sin(angle);

                foreach (Point profilePoint
                         in profile)
                {
                    Point3D point =
                        center
                        + axis * profilePoint.X
                        + radialDirection
                        * profilePoint.Y;

                    mesh.Positions.Add(
                        point);
                }
            }

            for (int segment = 0;
                 segment < segments;
                 segment++)
            {
                int nextSegment =
                    (segment + 1)
                    % segments;

                for (int profileIndex = 0;
                     profileIndex < profileCount;
                     profileIndex++)
                {
                    int nextProfile =
                        (profileIndex + 1)
                        % profileCount;

                    int a =
                        segment
                        * profileCount
                        + profileIndex;

                    int b =
                        nextSegment
                        * profileCount
                        + profileIndex;

                    int c =
                        segment
                        * profileCount
                        + nextProfile;

                    int d =
                        nextSegment
                        * profileCount
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
                new DiffuseMaterial(
                    brush);

            return new GeometryModel3D
            {
                Geometry = mesh,
                Material = material,
                BackMaterial = material
            };
        }


        private void CreateBigWheelToolboxPreview(
    Model3DGroup previewModel)
        {
            previewModel.Children.Clear();

            Part part =
                PartLibrary.Parts.FirstOrDefault(
                    p => p.Name == "Big Rad");

            if (!(part is BigWheel wheel))
                return;

            double previewScale = 0.5;

            Point3D center =
                new Point3D(0, 0, 0);

            Vector3D axis =
                new Vector3D(1, 0, 0);

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


            // ------------------------------------------------------------
            // REIFEN
            // ------------------------------------------------------------

            double tireOuterRadius =
                wheel.OuterDiameter
                / 200.0
                * previewScale;

            double tireInnerRadius =
                wheel.RimDiameter
                / 200.0
                * previewScale;

            double tireHalfWidth =
                wheel.TireWidth
                / 200.0
                * previewScale;

            GeometryModel3D tire =
                CreatePreviewFlatRing(
                    center,
                    axis,
                    tireOuterRadius,
                    tireInnerRadius,
                    tireHalfWidth,
                    Brushes.Black);

            if (tire != null)
                previewModel.Children.Add(tire);


            // ------------------------------------------------------------
            // ROTE FELGENSCHEIBE
            // ------------------------------------------------------------

            double rimOuterRadius =
                wheel.RimDiameter
                / 200.0
                * previewScale;

            double centerHoleRadius =
                wheel.HoleDiameter
                / 200.0
                * previewScale;

            double rimBodyHalfWidth =
                wheel.RimBodyThickness
                / 200.0
                * previewScale;

            GeometryModel3D rim =
                CreatePreviewFlatRing(
                    center,
                    axis,
                    rimOuterRadius,
                    centerHoleRadius,
                    rimBodyHalfWidth,
                    PaksyRed);

            if (rim != null)
                previewModel.Children.Add(rim);


            // ------------------------------------------------------------
            // ÄUSSERER FELGENRAND
            // ------------------------------------------------------------

            double rimEdgeInnerRadius =
                (
                    wheel.RimDiameter / 2.0
                    - wheel.RimEdgeWidth
                )
                / 100.0
                * previewScale;

            double rimEdgeHalfWidth =
                2.0
                / 100.0
                * previewScale;

            GeometryModel3D rimEdge =
                CreatePreviewFlatRing(
                    center,
                    axis,
                    rimOuterRadius,
                    rimEdgeInnerRadius,
                    rimEdgeHalfWidth,
                    PaksyRed);

            if (rimEdge != null)
                previewModel.Children.Add(rimEdge);


            // ------------------------------------------------------------
            // MITTLERE NABE
            // ------------------------------------------------------------

            double hubOuterRadius =
                7.0
                / 100.0
                * previewScale;

            double hubHalfWidth =
                wheel.BoreDepth
                / 200.0
                * previewScale;

            GeometryModel3D hub =
                CreatePreviewFlatRing(
                    center,
                    axis,
                    hubOuterRadius,
                    centerHoleRadius,
                    hubHalfWidth,
                    PaksyRed);

            if (hub != null)
                previewModel.Children.Add(hub);


            // ------------------------------------------------------------
            // VIER ZUSÄTZLICHE LÖCHER
            // ------------------------------------------------------------

            double sideHoleRadius =
                wheel.SideHoleRadius
                / 100.0
                * previewScale;

            double sideHoleTubeRadius =
                wheel.SideHoleDiameter
                / 200.0
                * previewScale;

            double holeHalfLength =
                2.5
                / 100.0
                * previewScale;

            for (int i = 0;
                 i < wheel.SideHoleCount;
                 i++)
            {
                double angle =
                    Math.PI / 4.0
                    + i * Math.PI / 2.0;

                Vector3D radial =
                    side1 * Math.Cos(angle)
                    + side2 * Math.Sin(angle);

                Point3D holeCenter =
                    center
                    + radial * sideHoleRadius;

                Point3D holeStart =
                    holeCenter
                    - axis * holeHalfLength;

                Point3D holeEnd =
                    holeCenter
                    + axis * holeHalfLength;

                GeometryModel3D hole =
                    CreatePreviewCylinder(
                        holeStart,
                        holeEnd,
                        sideHoleTubeRadius,
                        Brushes.Black);

                if (hole != null)
                {
                    previewModel.Children.Add(
                        hole);
                }
            }

            ApplyToolboxPreviewStartRotation(
                "Big Rad",
                previewModel);
        }



        private GeometryModel3D CreatePreviewFlatRing(
    Point3D center,
    Vector3D axis,
    double outerRadius,
    double innerRadius,
    double halfWidth,
    Brush brush)
        {
            const int segments = 64;

            if (axis.Length == 0)
                return null;

            if (outerRadius <= innerRadius ||
                innerRadius < 0 ||
                halfWidth <= 0)
            {
                return null;
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

            MeshGeometry3D mesh =
                new MeshGeometry3D();

            for (int i = 0;
                 i < segments;
                 i++)
            {
                double angle =
                    2.0 * Math.PI
                    * i
                    / segments;

                Vector3D radial =
                    side1 * Math.Cos(angle)
                    + side2 * Math.Sin(angle);

                mesh.Positions.Add(
                    center
                    - axis * halfWidth
                    + radial * outerRadius);

                mesh.Positions.Add(
                    center
                    - axis * halfWidth
                    + radial * innerRadius);

                mesh.Positions.Add(
                    center
                    + axis * halfWidth
                    + radial * outerRadius);

                mesh.Positions.Add(
                    center
                    + axis * halfWidth
                    + radial * innerRadius);
            }

            for (int i = 0;
                 i < segments;
                 i++)
            {
                int next =
                    (i + 1) % segments;

                int a = i * 4;
                int b = next * 4;

                // Vorderseite
                mesh.TriangleIndices.Add(a);
                mesh.TriangleIndices.Add(b);
                mesh.TriangleIndices.Add(a + 1);

                mesh.TriangleIndices.Add(a + 1);
                mesh.TriangleIndices.Add(b);
                mesh.TriangleIndices.Add(b + 1);

                // Rückseite
                mesh.TriangleIndices.Add(a + 2);
                mesh.TriangleIndices.Add(a + 3);
                mesh.TriangleIndices.Add(b + 2);

                mesh.TriangleIndices.Add(a + 3);
                mesh.TriangleIndices.Add(b + 3);
                mesh.TriangleIndices.Add(b + 2);

                // Außenfläche
                mesh.TriangleIndices.Add(a);
                mesh.TriangleIndices.Add(a + 2);
                mesh.TriangleIndices.Add(b);

                mesh.TriangleIndices.Add(b);
                mesh.TriangleIndices.Add(a + 2);
                mesh.TriangleIndices.Add(b + 2);

                // Innenfläche
                mesh.TriangleIndices.Add(a + 1);
                mesh.TriangleIndices.Add(b + 1);
                mesh.TriangleIndices.Add(a + 3);

                mesh.TriangleIndices.Add(a + 3);
                mesh.TriangleIndices.Add(b + 1);
                mesh.TriangleIndices.Add(b + 3);
            }

            DiffuseMaterial material =
                new DiffuseMaterial(
                    brush);

            return new GeometryModel3D
            {
                Geometry = mesh,
                Material = material,
                BackMaterial = material
            };
        }



        /// <summary>
        /// Berechnet einen rasterkonformen Drehpunkt für die aktuelle Auswahl.
        /// Der Pivot liegt immer auf einem echten 27,5-mm-Rasterpunkt
        /// und möglichst nah am geometrischen Schwerpunkt.
        /// </summary>
        private Vector3 GetRotationPivot(IList<PlacedPart> parts)
        {
            if (parts == null || parts.Count == 0)
                return new Vector3();

            double sumX = 0;
            double sumY = 0;
            double sumZ = 0;
            int count = 0;

            foreach (PlacedPart placed in parts)
            {
                double x = placed.Transform.Position.X / Scale;
                double y = placed.Transform.Position.Y / Scale;
                double z = placed.Transform.Position.Z;

                // Bei Platten den visuellen Mittelpunkt verwenden
                if (placed.Part is Plate)
                {
                    Vector3 plateOffset = GetPlateGridOffset(placed);
                    x += plateOffset.X;
                    y += plateOffset.Y;
                    z += plateOffset.Z;
                }

                sumX += x;
                sumY += y;
                sumZ += z;
                count++;
            }

            double avgX = sumX / count;
            double avgY = sumY / count;
            double avgZ = sumZ / count;

            // Auf den nächsten echten Rasterpunkt runden
            double grid = Grider.StepSize;

            double pivotX = Math.Round(avgX / grid) * grid;
            double pivotY = Math.Round(avgY / grid) * grid;
            double pivotZ = Math.Round(avgZ / grid) * grid;

            return new Vector3(pivotX, pivotY, pivotZ);
        }


        /// <summary>
        /// Findet den Socket eines Bauteils, der dem Mausklick am nächsten liegt.
        /// </summary>

        /// <summary>
        /// Gibt die komplementäre Face zurück (Left↔Right, Top↔Bottom, Front↔Back).
        /// </summary>
        private Face GetOppositeFace(Face face)
        {
            switch (face)
            {
                case Face.Left: return Face.Right;
                case Face.Right: return Face.Left;
                case Face.Top: return Face.Bottom;
                case Face.Bottom: return Face.Top;
                case Face.Front: return Face.Back;
                case Face.Back: return Face.Front;
                default: return face;
            }
        }

        /// <summary>
        /// Versucht eine 90°-Orientierung zu finden, bei der der Socket
        /// des neuen Teils die gewünschte Welt-Face bekommt.
        /// </summary>
        private bool TryFindOrientationForSocket(
            Part newPart,
            Socket newSocket,
            Face targetWorldFace,
            out Vector3 rotation,
            out int legacyRotation)
        {
            rotation = new Vector3(0, 0, 0);
            legacyRotation = 0;

            // Alle 24 möglichen 90°-Orientierungen durchprobieren
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    for (int z = 0; z < 4; z++)
                    {
                        Vector3 candidate = new Vector3(x * 90, y * 90, z * 90);

                        Face effective = FaceHelper.RotateFace3D(
                            newSocket.Face,
                            candidate);

                        if (effective == targetWorldFace)
                        {
                            rotation = candidate;
                            // Legacy-Rotation (nur Z) für ältere 2D-Logik
                            legacyRotation = z * 90;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Platziert das aktuell in der Toolbox ausgewählte Bauteil
        /// an den nächstgelegenen Socket des angeklickten Bauteils.
        /// </summary>
        /// <summary>
        /// Platziert das aktuell ausgewählte Toolbox-Bauteil
        /// an den nächstgelegenen Socket des angeklickten Bauteils.
        /// </summary>
        /// <summary>
        /// Platziert das Toolbox-Bauteil an den nächstgelegenen Socket
        /// des angeklickten Bauteils.
        /// </summary>
        /// <summary>
        /// Platziert das Toolbox-Bauteil an einem freien Socket
        /// des angeklickten Bauteils.
        /// </summary>
        private void PlaceSelectedPartAtSocket(
            PlacedPart targetPart,
            Point3D hitPointInWorld)
        {
            if (selectedPart == null)
            {
                StatusText.Text = "Kein Bauteil in der Toolbox ausgewählt";
                return;
            }

            if (targetPart == null)
                return;

            // --------------------------------------------------------
            // 1. Alle Sockets des Ziels sammeln (freie bevorzugt)
            // --------------------------------------------------------
            List<Socket> candidateSockets = targetPart.Sockets
                .OrderBy(s => s.IsConnected ? 1 : 0)   // freie zuerst
                .ToList();

            if (candidateSockets.Count == 0)
            {
                StatusText.Text = "Zielbauteil hat keine Sockets";
                return;
            }

            // --------------------------------------------------------
            // 2. Jeden Socket ausprobieren, bis eine freie Position gefunden wird
            // --------------------------------------------------------
            PlacedPart bestPlaced = null;
            Vector3 bestPosition = null;
            double bestDistance = double.MaxValue;

            foreach (Socket targetSocket in candidateSockets)
            {
                // Effektive Face des Ziel-Sockets
                Face targetWorldFace = FaceHelper.RotateFace(
                    targetSocket.Face,
                    targetPart.Rotation);

                targetWorldFace = FaceHelper.RotateFace3D(
                    targetWorldFace,
                    targetPart.Transform.Rotation);

                Face requiredFace = GetOppositeFace(targetWorldFace);

                // Neues Bauteil vorbereiten
                PlacedPart candidate = new PlacedPart
                {
                    Part = selectedPart,
                    Transform = new PlastiCAD.Models.Transform(),
                    Sockets = selectedPart.CreateSockets(),
                    Rotation = 0
                };

                // Passende Orientierung suchen
                Socket bestNewSocket = null;
                Vector3 bestRotation = new Vector3(0, 0, 0);
                int bestLegacyRotation = 0;
                bool orientationFound = false;

                foreach (Socket newSocket in candidate.Sockets)
                {
                    if (TryFindOrientationForSocket(
                            selectedPart,
                            newSocket,
                            requiredFace,
                            out Vector3 rotation,
                            out int legacyRot))
                    {
                        bestNewSocket = newSocket;
                        bestRotation = rotation;
                        bestLegacyRotation = legacyRot;
                        orientationFound = true;
                        break;
                    }
                }

                if (!orientationFound || bestNewSocket == null)
                    continue;

                candidate.Transform.Rotation = bestRotation;
                candidate.Rotation = bestLegacyRotation;

                // Idealposition berechnen
                double halfCell = Grider.CellSize / 2.0;

                Vector3 targetSocketMm = SnapEngine.GetSocketWorldPosition(
                    targetPart,
                    targetSocket,
                    Scale);

                Vector3 offsetFromCenter = GetSocketOffsetFromCenter(
                    bestNewSocket,
                    candidate);

                double newCenterX = targetSocketMm.X - offsetFromCenter.X;
                double newCenterY = targetSocketMm.Y - offsetFromCenter.Y;
                double newCenterZ = targetSocketMm.Z - offsetFromCenter.Z;

                double posX = (newCenterX - halfCell) * Scale;
                double posY = (newCenterY - halfCell) * Scale;
                double posZ = newCenterZ;

                // Auf Raster runden
                double grid = Grider.StepSize * Scale;
                posX = Math.Round(posX / grid) * grid;
                posY = Math.Round(posY / grid) * grid;
                posZ = Math.Round(posZ / Grider.StepSize) * Grider.StepSize;

                Vector3 idealPos = new Vector3(posX, posY, posZ);

                // Ist die Idealposition frei?
                if (IsPositionFree(idealPos, candidate))
                {
                    // Abstand zum Klickpunkt (für „nächster freier Socket“)
                    double dist = DistanceToHit(idealPos, hitPointInWorld);

                    if (dist < bestDistance)
                    {
                        bestDistance = dist;
                        bestPosition = idealPos;
                        bestPlaced = candidate;
                        bestPlaced.Transform.Position = idealPos;
                    }
                }
            }

            // --------------------------------------------------------
            // 3. Falls kein Socket eine freie Idealposition hatte:
            //    räumlich um den besten Kandidaten suchen
            // --------------------------------------------------------
            if (bestPlaced == null)
            {
                // Nimm den ersten Socket und suche räumlich
                Socket fallbackSocket = candidateSockets[0];

                Face targetWorldFace = FaceHelper.RotateFace(
                    fallbackSocket.Face,
                    targetPart.Rotation);
                targetWorldFace = FaceHelper.RotateFace3D(
                    targetWorldFace,
                    targetPart.Transform.Rotation);

                Face requiredFace = GetOppositeFace(targetWorldFace);

                bestPlaced = new PlacedPart
                {
                    Part = selectedPart,
                    Transform = new PlastiCAD.Models.Transform(),
                    Sockets = selectedPart.CreateSockets(),
                    Rotation = 0
                };

                Socket bestNewSocket = null;
                foreach (Socket newSocket in bestPlaced.Sockets)
                {
                    if (TryFindOrientationForSocket(
                            selectedPart,
                            newSocket,
                            requiredFace,
                            out Vector3 rotation,
                            out int legacyRot))
                    {
                        bestNewSocket = newSocket;
                        bestPlaced.Transform.Rotation = rotation;
                        bestPlaced.Rotation = legacyRot;
                        break;
                    }
                }

                if (bestNewSocket == null)
                {
                    StatusText.Text = "Keine passende Orientierung gefunden";
                    return;
                }

                double halfCell = Grider.CellSize / 2.0;
                Vector3 targetSocketMm = SnapEngine.GetSocketWorldPosition(
                    targetPart, fallbackSocket, Scale);
                Vector3 offset = GetSocketOffsetFromCenter(bestNewSocket, bestPlaced);

                double posX = (targetSocketMm.X - offset.X - halfCell) * Scale;
                double posY = (targetSocketMm.Y - offset.Y - halfCell) * Scale;
                double posZ = targetSocketMm.Z - offset.Z;

                double grid = Grider.StepSize * Scale;
                posX = Math.Round(posX / grid) * grid;
                posY = Math.Round(posY / grid) * grid;
                posZ = Math.Round(posZ / Grider.StepSize) * Grider.StepSize;

                bestPosition = FindNearestFreePosition(
                    new Vector3(posX, posY, posZ),
                    bestPlaced);

                bestPlaced.Transform.Position = bestPosition;
            }

            // --------------------------------------------------------
            // 4. Einfügen
            // --------------------------------------------------------
            SaveUndoState();

            assembly.PlacedParts.Add(bestPlaced);

            selectedParts.Clear();
            selectedParts.Add(bestPlaced);

            int connections = ConnectSelectedParts();

            StatusText.Text = connections > 0
                ? $"Bauteil eingefügt – {connections} Verbindung(en)"
                : "Bauteil eingefügt";

            RedrawScene();
        }

        /// <summary>
        /// Grober Abstand der Rasterposition zum 3D-Klickpunkt
        /// (nur zur Auswahl des „nächsten“ freien Sockets).
        /// </summary>
        private double DistanceToHit(Vector3 position, Point3D hitPoint)
        {
            double wx = (position.X / Scale + Grider.CellSize / 2.0) / 100.0;
            double wy = -(position.Y / Scale + Grider.CellSize / 2.0) / 100.0;
            double wz = position.Z / 100.0;

            double dx = wx - hitPoint.X;
            double dy = wy - hitPoint.Y;
            double dz = wz - hitPoint.Z;

            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
        /// <summary>
        /// Liefert den Offset eines Sockets vom Zellen-Mittelpunkt
        /// in Modell-mm, unter Berücksichtigung der aktuellen Orientierung.
        /// </summary>
        private Vector3 GetSocketOffsetFromCenter(
    Socket socket,
    PlacedPart placed)
        {
            double r = Grider.CellSize / 2.0;   // 13.75

            // Nur Transform.Rotation – nicht zusätzlich Legacy-Rotation
            Face face = FaceHelper.RotateFace3D(
                socket.Face,
                placed.Transform.Rotation);

            switch (face)
            {
                case Face.Left: return new Vector3(-r, 0, 0);
                case Face.Right: return new Vector3(r, 0, 0);
                case Face.Top: return new Vector3(0, -r, 0);
                case Face.Bottom: return new Vector3(0, r, 0);
                case Face.Front: return new Vector3(0, 0, r);
                case Face.Back: return new Vector3(0, 0, -r);
                default: return new Vector3(0, 0, 0);
            }
        }


        private Socket FindNearestSocket(PlacedPart placed, Point3D hitPointInWorld)
        {
            // Vorerst: einfach den ersten freien Socket nehmen
            // oder den, der am weitesten „nach außen“ zeigt.
            // Später können wir den echten Hit-Punkt nutzen.

            if (placed?.Sockets == null || placed.Sockets.Count == 0)
                return null;

            // Bevorzugt einen noch nicht verbundenen Socket
            Socket free = placed.Sockets.FirstOrDefault(s => !s.IsConnected);
            if (free != null)
                return free;

            return placed.Sockets[0];
        }


        /// <summary>
        /// Sucht die nächstliegende freie Rasterposition.
        /// Beginnt bei der Idealposition und prüft spiralförmig
        /// die umliegenden Zellen (inkl. Z-Ebenen).
        /// </summary>
        /// <summary>
        /// Sucht die nächstliegende freie Rasterposition.
        /// Beginnt bei der Idealposition und erweitert den Radius
        /// so lange, bis eine freie Zelle gefunden wird.
        /// </summary>
        private Vector3 FindNearestFreePosition(
            Vector3 idealPosition,
            PlacedPart newPart)
        {
            double grid = Grider.StepSize * Scale;   // Canvas-Einheiten in X/Y
            double gridZ = Grider.StepSize;          // Z in mm

            // Idealposition zuerst prüfen
            if (IsPositionFree(idealPosition, newPart))
                return idealPosition;

            // Radius so lange erhöhen, bis etwas frei ist
            int radius = 1;

            while (true)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        for (int dz = -radius; dz <= radius; dz++)
                        {
                            // Nur die äußere Schale dieses Radius prüfen
                            if (Math.Max(Math.Max(Math.Abs(dx), Math.Abs(dy)), Math.Abs(dz)) != radius)
                                continue;

                            Vector3 candidate = new Vector3(
                                idealPosition.X + dx * grid,
                                idealPosition.Y + dy * grid,
                                idealPosition.Z + dz * gridZ);

                            if (IsPositionFree(candidate, newPart))
                                return candidate;
                        }
                    }
                }

                radius++;
            }
        }

        /// <summary>
        /// Prüft, ob an dieser Position bereits ein (nicht-Overlay-)Bauteil liegt.
        /// </summary>
        private bool IsPositionFree(Vector3 position, PlacedPart newPart)
        {
            const double tolerance = 0.5;

            foreach (PlacedPart existing in assembly.PlacedParts)
            {
                // Overlay-Teile (Platten etc.) dürfen sich mit Strukturteilen überlappen
                bool existingIsOverlay = existing.Part is Plate || existing.Part is BigPlate;
                bool newIsOverlay = newPart.Part is Plate || newPart.Part is BigPlate;

                if (existingIsOverlay != newIsOverlay)
                    continue;   // unterschiedliche Typen dürfen dieselbe Zelle teilen

                if (Math.Abs(existing.Transform.Position.X - position.X) < tolerance &&
                    Math.Abs(existing.Transform.Position.Y - position.Y) < tolerance &&
                    Math.Abs(existing.Transform.Position.Z - position.Z) < tolerance)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Startet oder wechselt die Socket-Auswahl am angeklickten Bauteil.
        /// Jeder weitere Linksklick auf dasselbe Teil wählt den nächsten freien Socket.
        /// </summary>
        private void HandleSocketSelectionClick(PlacedPart clickedPart, Point3D hitPoint)
        {
            // Verbindungen aktuell halten
            RebuildConnections();

            // Auswahl aufheben – Socket-Modus aktiv
            selectedParts.Clear();

            // Neues Zielbauteil?
            if (socketTargetPart != clickedPart)
            {
                HideSocketMarker();

                socketTargetPart = clickedPart;

                socketTargetCandidates = clickedPart.Sockets
                    .Where(s => !s.IsConnected)
                    .ToList();

                if (socketTargetCandidates.Count == 0)
                {
                    StatusText.Text = "Dieses Bauteil hat keine freien Anschlüsse";
                    socketTargetPart = null;
                    socketTargetIndex = -1;
                    RedrawScene();   // Auswahl-Highlight entfernen
                    return;
                }

                socketTargetIndex = FindNearestSocketIndex(clickedPart, hitPoint);
            }
            else
            {
                socketTargetCandidates = clickedPart.Sockets
                    .Where(s => !s.IsConnected)
                    .ToList();

                if (socketTargetCandidates.Count == 0)
                {
                    HideSocketMarker();
                    StatusText.Text = "Keine freien Anschlüsse mehr";
                    socketTargetPart = null;
                    socketTargetIndex = -1;
                    RedrawScene();
                    return;
                }

                socketTargetIndex = (socketTargetIndex + 1) % socketTargetCandidates.Count;
            }

            Socket currentSocket = socketTargetCandidates[socketTargetIndex];

            ShowSocketMarker(clickedPart, currentSocket);

            // Szene neu zeichnen, damit die alte Auswahl-Markierung verschwindet,
            // der gelbe Zylinder aber erhalten bleibt (am Ende von RedrawWorld neu setzen)
            RedrawScene();

            StatusText.Text =
                $"Freier Socket {socketTargetIndex + 1}/{socketTargetCandidates.Count} " +
                $"({currentSocket.Name ?? currentSocket.Face.ToString()}) – " +
                "Linksklick = platzieren, Rechtsklick = nächster freier Socket, Esc = Abbruch";
        }

        /// <summary>
        /// Findet den Index des freien Sockets, der dem 3D-Klickpunkt am nächsten liegt.
        /// </summary>
        private int FindNearestSocketIndex(PlacedPart part, Point3D hitPoint)
        {
            int bestIndex = 0;
            double bestDistance = double.MaxValue;

            for (int i = 0; i < socketTargetCandidates.Count; i++)
            {
                Socket socket = socketTargetCandidates[i];

                Vector3 socketMm = SnapEngine.GetSocketWorldPosition(part, socket, Scale);

                // Modell-mm → WPF-Weltkoordinaten (wie im Rest des Programms)
                double wx = (socketMm.X) / 100.0;   // GetSocketWorldPosition liefert schon in Modell-mm
                double wy = -(socketMm.Y) / 100.0;
                double wz = socketMm.Z / 100.0;

                // Korrektur: GetSocketWorldPosition arbeitet mit Zellenmitte
                // → wir nehmen die gleiche Umrechnung wie beim Pivot
                double halfGrid = Grider.CellSize / 2.0;
                wx = (socketMm.X) / 100.0;
                wy = -(socketMm.Y) / 100.0;
                wz = socketMm.Z / 100.0;

                double dx = wx - hitPoint.X;
                double dy = wy - hitPoint.Y;
                double dz = wz - hitPoint.Z;

                double distance = Math.Sqrt(dx * dx + dy * dy + dz * dz);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        /// <summary>
        /// Zeigt einen gelben Zylinder (1 cm = Verbindungsglied) am gewählten Socket.
        /// </summary>
        private void ShowSocketMarker(PlacedPart part, Socket socket)
        {
            HideSocketMarker();

            // Socket-Position in Modell-mm
            Vector3 socketMm = SnapEngine.GetSocketWorldPosition(part, socket, Scale);

            // Effektive Face → Richtung nach außen
            Face worldFace = FaceHelper.RotateFace(socket.Face, part.Rotation);
            worldFace = FaceHelper.RotateFace3D(worldFace, part.Transform.Rotation);

            Vector3D direction = GetDirectionVector(worldFace);

            // Weltkoordinaten (WPF): X = mm/100, Y = -mm/100, Z = mm/100
            Point3D start = new Point3D(
                socketMm.X / 100.0,
                -socketMm.Y / 100.0,
                socketMm.Z / 100.0);

            // 1 cm = 0.01 in WPF-Einheiten rausstehen lassen
            const double length = 0.10;   // 1 cm
            const double radius = 0.04;   // Ø 8 mm  // ca. 2,5 mm Radius (sieht nach Verbindungsglied aus)

            Point3D end = new Point3D(
                start.X + direction.X * length,
                start.Y + direction.Y * length,
                start.Z + direction.Z * length);

            Brush yellow = new SolidColorBrush(Color.FromRgb(255, 200, 0));
            yellow.Freeze();

            GeometryModel3D cylinder = CreatePreviewCylinder(start, end, radius, yellow);

            if (cylinder == null)
                return;

            socketMarkerVisual = new ModelVisual3D
            {
                Content = cylinder
            };

            WorldViewport.Children.Add(socketMarkerVisual);
        }

        /// <summary>
        /// Entfernt den gelben Socket-Marker.
        /// </summary>
        private void HideSocketMarker()
        {
            if (socketMarkerVisual != null)
            {
                WorldViewport.Children.Remove(socketMarkerVisual);
                socketMarkerVisual = null;
            }
        }

        /// <summary>
        /// Liefert die Weltrichtung einer Face als Vector3D (für den Zylinder).
        /// Beachte: Y ist in WPF gespiegelt.
        /// </summary>
        private Vector3D GetDirectionVector(Face face)
        {
            switch (face)
            {
                case Face.Left: return new Vector3D(-1, 0, 0);
                case Face.Right: return new Vector3D(1, 0, 0);
                case Face.Top: return new Vector3D(0, 1, 0);  // WPF-Y zeigt nach oben → Top = +Y
                case Face.Bottom: return new Vector3D(0, -1, 0);
                case Face.Front: return new Vector3D(0, 0, 1);
                case Face.Back: return new Vector3D(0, 0, -1);
                default: return new Vector3D(1, 0, 0);
            }
        }

        /// <summary>
        /// Platziert das aktuell in der Toolbox gewählte Bauteil
        /// am gerade markierten Socket.
        /// </summary>
        private void ConfirmSocketPlacement()
        {
            if (socketTargetPart == null ||
                socketTargetIndex < 0 ||
                socketTargetIndex >= socketTargetCandidates.Count ||
                selectedPart == null)
            {
                return;
            }

            Socket targetSocket = socketTargetCandidates[socketTargetIndex];

            PlaceSelectedPartAtSpecificSocket(socketTargetPart, targetSocket);

            // Aufräumen
            HideSocketMarker();
            socketTargetPart = null;
            socketTargetCandidates.Clear();
            socketTargetIndex = -1;
        }

        /// <summary>
        /// Bricht die Socket-Auswahl ab.
        /// </summary>
        private void CancelSocketSelection()
        {
            HideSocketMarker();
            socketTargetPart = null;
            socketTargetCandidates.Clear();
            socketTargetIndex = -1;
            StatusText.Text = "Socket-Auswahl abgebrochen";
        }


        /// <summary>
        /// Platziert das aktuell in der Toolbox ausgewählte Bauteil
        /// genau an den angegebenen Socket des Zielbauteils.
        /// </summary>
        private void PlaceSelectedPartAtSpecificSocket(
            PlacedPart targetPart,
            Socket targetSocket)
        {
            if (selectedPart == null)
            {
                StatusText.Text = "Kein Bauteil in der Toolbox ausgewählt";
                return;
            }

            if (targetPart == null || targetSocket == null)
                return;

            // --------------------------------------------------------
            // 1. Effektive Welt-Face des Ziel-Sockets
            // --------------------------------------------------------
            Face targetWorldFace = FaceHelper.RotateFace(
                targetSocket.Face,
                targetPart.Rotation);

            targetWorldFace = FaceHelper.RotateFace3D(
                targetWorldFace,
                targetPart.Transform.Rotation);

            // Das neue Bauteil braucht die gegenüberliegende Face
            Face requiredFace = GetOppositeFace(targetWorldFace);

            // --------------------------------------------------------
            // 2. Neues Bauteil anlegen und passende Orientierung suchen
            // --------------------------------------------------------
            PlacedPart newPlaced = new PlacedPart
            {
                Part = selectedPart,
                Transform = new PlastiCAD.Models.Transform(),
                Sockets = selectedPart.CreateSockets(),
                Rotation = 0
            };

            Socket matchingSocket = null;
            Vector3 foundRotation = new Vector3(0, 0, 0);
            int foundLegacyRotation = 0;
            bool orientationFound = false;

            foreach (Socket newSocket in newPlaced.Sockets)
            {
                if (TryFindOrientationForSocket(
                        selectedPart,
                        newSocket,
                        requiredFace,
                        out Vector3 rotation,
                        out int legacyRot))
                {
                    matchingSocket = newSocket;
                    foundRotation = rotation;
                    foundLegacyRotation = legacyRot;
                    orientationFound = true;
                    break;
                }
            }

            if (!orientationFound || matchingSocket == null)
            {
                StatusText.Text = "Keine passende Orientierung für diesen Socket gefunden";
                return;
            }

            newPlaced.Transform.Rotation = foundRotation;
            newPlaced.Rotation = foundLegacyRotation;

            // --------------------------------------------------------
            // 3. Idealposition berechnen
            // --------------------------------------------------------
            double halfCell = Grider.CellSize / 2.0;

            // Weltposition des Ziel-Sockets (in Modell-mm)
            Vector3 targetSocketMm = SnapEngine.GetSocketWorldPosition(
                targetPart,
                targetSocket,
                Scale);

            // Offset des Matching-Sockets vom Zellenmittelpunkt des neuen Teils
            Vector3 offsetFromCenter = GetSocketOffsetFromCenter(
                matchingSocket,
                newPlaced);

            // Zellenmittelpunkt des neuen Teils so legen,
            // dass sein Socket genau auf dem Ziel-Socket liegt
            double newCenterX = targetSocketMm.X - offsetFromCenter.X;
            double newCenterY = targetSocketMm.Y - offsetFromCenter.Y;
            double newCenterZ = targetSocketMm.Z - offsetFromCenter.Z;

            // Transform.Position speichert die Zellen-Ecke (nicht die Mitte)
            double posX = (newCenterX - halfCell) * Scale;
            double posY = (newCenterY - halfCell) * Scale;
            double posZ = newCenterZ;

            // Auf Raster runden
            double grid = Grider.StepSize * Scale;
            posX = Math.Round(posX / grid) * grid;
            posY = Math.Round(posY / grid) * grid;
            posZ = Math.Round(posZ / Grider.StepSize) * Grider.StepSize;

            Vector3 idealPosition = new Vector3(posX, posY, posZ);

            // --------------------------------------------------------
            // 4. Freie Position finden (Ideal oder nächste freie Zelle)
            // --------------------------------------------------------
            Vector3 finalPosition;

            if (IsPositionFree(idealPosition, newPlaced))
            {
                finalPosition = idealPosition;
            }
            else
            {
                finalPosition = FindNearestFreePosition(idealPosition, newPlaced);
            }

            newPlaced.Transform.Position = finalPosition;

            // --------------------------------------------------------
            // 5. Einfügen und verbinden
            // --------------------------------------------------------
            SaveUndoState();

            assembly.PlacedParts.Add(newPlaced);

            selectedParts.Clear();
            selectedParts.Add(newPlaced);

            int connections = ConnectSelectedParts();

            StatusText.Text = connections > 0
                ? $"Bauteil eingefügt – {connections} Verbindung(en)"
                : "Bauteil eingefügt";

            // Toolbox-Auswahl optional zurücksetzen
            // (auskommentieren, wenn du mehrere Teile hintereinander setzen willst)
            // selectedPart = null;
            // if (selectedPartToolButton != null) { ... Reset-Button-Style ... }

            RedrawScene();
        }


        private void MenuFullscreenAnimation_Click(object sender, RoutedEventArgs e)
        {
            StartFullscreenAnimation();
        }

        private void StartFullscreenAnimation()
        {
            if (assembly.PlacedParts.Count == 0)
            {
                StatusText.Text = "Kein Modell für die Vollbildanimation";
                return;
            }

            MainTabs.SelectedItem = WorldTab;
            RedrawWorld();

            GetFullscreenOrbit(out Point3D target, out double distance, out double height);

            FullscreenAnimationWindow window = new FullscreenAnimationWindow();
            window.Owner = this;
            window.Closed += (s, e) =>
            {
                RedrawWorld();
                StatusText.Text = "Vollbildanimation beendet";
            };

            window.Show();
            window.Start(WorldViewport, target, distance, height);
        }

        private void GetFullscreenOrbit(
            out Point3D target,
            out double distance,
            out double height)
        {
            double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;
            double half = (Grider.CellSize / 2.0) / 100.0;

            foreach (PlacedPart placed in assembly.PlacedParts)
            {
                double x = (placed.Transform.Position.X / Scale + Grider.CellSize / 2.0) / 100.0;
                double y = -(placed.Transform.Position.Y / Scale + Grider.CellSize / 2.0) / 100.0;
                double z = placed.Transform.Position.Z / 100.0;

                minX = Math.Min(minX, x - half);
                maxX = Math.Max(maxX, x + half);
                minY = Math.Min(minY, y - half);
                maxY = Math.Max(maxY, y + half);
                minZ = Math.Min(minZ, z - half);
                maxZ = Math.Max(maxZ, z + half);
            }

            target = new Point3D(
                (minX + maxX) / 2.0,
                (minY + maxY) / 2.0,
                (minZ + maxZ) / 2.0);

            double sizeX = Math.Max(maxX - minX, 0.2);
            double sizeY = Math.Max(maxY - minY, 0.2);
            double sizeZ = Math.Max(maxZ - minZ, 0.2);

            double radius = 0.5 * Math.Sqrt(sizeX * sizeX + sizeY * sizeY + sizeZ * sizeZ);

            // 45° FOV im Vollbildfenster, Modell so groß wie möglich
            double fov = 45.0 * Math.PI / 180.0;
            distance = radius / Math.Tan(fov / 2.0) * 1.05;
            height = radius * 0.12;
        }
        private void StopFullscreenAnimation()
        {
            if (!isFullscreenAnimation)
                return;

            if (fullscreenAnimationTimer != null)
            {
                fullscreenAnimationTimer.Stop();
                fullscreenAnimationTimer.Tick -= FullscreenAnimationTimer_Tick;
                fullscreenAnimationTimer = null;
            }

            Topmost = false;
            WindowStyle = fullscreenSavedWindowStyle;
            ResizeMode = fullscreenSavedResizeMode;
            WindowState = fullscreenSavedWindowState;

            if (FileMenu.Parent is FrameworkElement menuBar)
                menuBar.Visibility = Visibility.Collapsed;

            if (PlanToolbar != null &&
                PlanToolbar.Parent is FrameworkElement toolbar)
                toolbar.Visibility = Visibility.Collapsed;

            StatusText.Visibility = Visibility.Collapsed;

            if (StatusText.Parent is FrameworkElement statusBar)
                statusBar.Visibility = Visibility.Collapsed;

            ((Grid)Content).ColumnDefinitions[0].Width = fullscreenSavedToolboxWidth;
            Grid.SetColumn(MainTabs, 1);
            Grid.SetColumnSpan(MainTabs, 1);
            MainTabs.Margin = new Thickness(5);
            MainTabs.ItemContainerStyle = null;

            isFullscreenAnimation = false;
            StatusText.Text = "Vollbildanimation beendet";
        }

        private void FitWorldCameraFullscreen()
        {
            if (assembly.PlacedParts.Count == 0)
                return;

            double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;
            double half = (Grider.CellSize / 2.0) / 100.0;

            foreach (PlacedPart placed in assembly.PlacedParts)
            {
                double x = (placed.Transform.Position.X / Scale + Grider.CellSize / 2.0) / 100.0;
                double y = -(placed.Transform.Position.Y / Scale + Grider.CellSize / 2.0) / 100.0;
                double z = placed.Transform.Position.Z / 100.0;

                minX = Math.Min(minX, x - half);
                maxX = Math.Max(maxX, x + half);
                minY = Math.Min(minY, y - half);
                maxY = Math.Max(maxY, y + half);
                minZ = Math.Min(minZ, z - half);
                maxZ = Math.Max(maxZ, z + half);
            }

            double centerX = (minX + maxX) / 2.0;
            double centerY = (minY + maxY) / 2.0;
            double centerZ = (minZ + maxZ) / 2.0;

            double sizeX = Math.Max(maxX - minX, 0.2);
            double sizeY = Math.Max(maxY - minY, 0.2);
            double sizeZ = Math.Max(maxZ - minZ, 0.2);

            // Bounding-Sphere, damit das Modell beim Drehen nicht abgeschnitten wird
            double radius = 0.5 * Math.Sqrt(sizeX * sizeX + sizeY * sizeY + sizeZ * sizeZ);

            double width = Math.Max(WorldViewport.ActualWidth, 1);
            double height = Math.Max(WorldViewport.ActualHeight, 1);
            double aspect = width / height;

            double verticalFov = WorldCamera.FieldOfView * Math.PI / 180.0;
            double horizontalFov = 2.0 * Math.Atan(Math.Tan(verticalFov / 2.0) * aspect);
            double limitingFov = Math.Min(verticalFov, horizontalFov);

            // So nah wie möglich, mit 6 % Rand
            double distance = radius / Math.Tan(limitingFov / 2.0) * 1.06;

            fullscreenOrbitTarget = new Point3D(centerX, centerY, centerZ);
            fullscreenOrbitDistance = distance;
            fullscreenOrbitHeight = radius * 0.18;
            fullscreenOrbitAngle = Math.PI * 0.25;

            Point3D position = new Point3D(
                centerX + Math.Cos(fullscreenOrbitAngle) * distance,
                centerY + fullscreenOrbitHeight,
                centerZ + Math.Sin(fullscreenOrbitAngle) * distance);

            WorldCamera.Position = position;
            WorldCamera.LookDirection = fullscreenOrbitTarget - position;
            WorldCamera.UpDirection = new Vector3D(0, 1, 0);
        }

        private void PrepareFullscreenOrbit()
        {
            Vector3D offset = WorldCamera.Position - fullscreenOrbitTarget;
            fullscreenOrbitDistance = Math.Sqrt(offset.X * offset.X + offset.Z * offset.Z);
            fullscreenOrbitHeight = offset.Y;
            fullscreenOrbitAngle = Math.Atan2(offset.Z, offset.X);
        }

        private void FullscreenAnimationTimer_Tick(object sender, EventArgs e)
        {
            fullscreenOrbitAngle += 0.0025;

            Point3D position = new Point3D(
                fullscreenOrbitTarget.X + Math.Cos(fullscreenOrbitAngle) * fullscreenOrbitDistance,
                fullscreenOrbitTarget.Y + fullscreenOrbitHeight,
                fullscreenOrbitTarget.Z + Math.Sin(fullscreenOrbitAngle) * fullscreenOrbitDistance);

            WorldCamera.Position = position;
            WorldCamera.LookDirection = fullscreenOrbitTarget - position;
            WorldCamera.UpDirection = new Vector3D(0, 1, 0);
        }

        







    }
}




