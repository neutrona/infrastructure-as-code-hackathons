using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

#region NShape
using Dataweb;
using Dataweb.NShape;
using Dataweb.NShape.Advanced;
using Dataweb.NShape.Layouters;
using Dataweb.NShape.GeneralShapes;
#endregion

namespace shift.ui.display
{
    class NShapeHelper
    {

        public enum LayouterType
        {
            Expansion,
            Flow,
            Grid,
            Repulsion,
        }

        public static Shape DrawCircle(int Diameter, int X, int Y, IFillStyle FillStyle, ILineStyle LineStyle,
                        String Caption, Object Tag, Char SecurityDomain, Dataweb.NShape.WinFormsUI.Display Display,
                        Dataweb.NShape.Project Project, Layer Layer)
        {
            CircleBase shape;
            shape = (CircleBase)Project.ShapeTypes["Circle"].CreateInstance();
            shape.Diameter = Diameter;

            shape.X = X;

            shape.Y = Y;

            shape.FillStyle = FillStyle;

            shape.SetCaptionText(0, Caption);

            shape.CharacterStyle = Project.Design.CharacterStyles.Heading3;

            shape.Tag = Tag;

            shape.SecurityDomainName = SecurityDomain;


            if (Display.InvokeRequired)
            {
                Display.BeginInvoke(new MethodInvoker(() => AddShape(Display, Project, shape)));
            }
            else
            {

                Display.Diagram.Shapes.Add(shape);
                Display.Diagram.AddShapeToLayers(shape, Layer.Id);

                Project.Repository.Insert((Shape)shape, Display.Diagram);
            }


            return shape;
        }

        public static void AddShape(Dataweb.NShape.WinFormsUI.Display Display, Dataweb.NShape.Project Project, Shape shape)
        {
            Display.Diagram.Shapes.Add(shape);
            Project.Repository.Insert(shape, Display.Diagram);
        }

        public static Shape DrawCircleRadialTree(int Diameter, int LayerShapeCount, int LayerShapeIndex, int LayerDistanceFromCenter, Point LayerCenter,
                                                    IFillStyle FillStyle, ILineStyle LineStyle, String Caption, Object Tag, Char SecurityDomain,
                                                    Dataweb.NShape.WinFormsUI.Display Display, Dataweb.NShape.Project Project, Layer Layer)
        {

            Point DiagramCenter = new Point(Display.Diagram.Width / 2, Display.Diagram.Height / 2);

            Point result = new Point();

            double angle = 0;

            if (LayerShapeCount == 1)
            {
                angle = 2 * Math.PI / LayerShapeCount; //between 0 and 2 * PI (~6.2832), angle is in radians

                if (LayerCenter.X < DiagramCenter.X)
                {
                    angle += Math.PI;
                }
            }

            if (LayerShapeCount > 1)
            {
                angle = 2 * Math.PI / LayerShapeCount * LayerShapeIndex; //between 0 and 2 * PI (~6.2832), angle is in radians

                if (LayerCenter.X < DiagramCenter.X)
                {
                    angle = (Math.PI / LayerShapeCount * LayerShapeIndex) + (0.5 * Math.PI);
                }

                if (LayerCenter.X > DiagramCenter.X)
                {
                    angle = ((Math.PI / LayerShapeCount) * LayerShapeIndex) + (1.5 * Math.PI);
                }
            }

            result.Y = (int)Math.Round(LayerCenter.Y + LayerDistanceFromCenter * Math.Sin(angle));
            result.X = (int)Math.Round(LayerCenter.X + LayerDistanceFromCenter * Math.Cos(angle));

            return DrawCircle(Diameter, result.X, result.Y, FillStyle, LineStyle, Caption, Tag, SecurityDomain, Display, Project, Layer);
        }

        public static Shape DrawBox(int Height, int Width, int X, int Y, IFillStyle FillStyle, ILineStyle LineStyle, ICharacterStyle CharacterStyle, IParagraphStyle ParagraphStyle,
                        String Caption, Object Tag, Char SecurityDomain, Dataweb.NShape.WinFormsUI.Display Display, Dataweb.NShape.Project Project)
        {
            Box shape;
            shape = (Box)Project.ShapeTypes["Box"].CreateInstance();
            shape.Height = Height;
            shape.Width = Width;

            shape.X = X;

            shape.Y = Y;

            shape.FillStyle = FillStyle;

            shape.CharacterStyle = CharacterStyle;

            shape.ParagraphStyle = ParagraphStyle;

            shape.SetCaptionText(0, Caption);

            shape.Tag = Tag;

            shape.SecurityDomainName = SecurityDomain;

            Display.Diagram.Shapes.Add(shape);

            Project.Repository.Insert((Shape)shape, Display.Diagram);

            return shape;
        }

        public static Shape DrawLabel(int Height, int Width, int X, int Y, IFillStyle FillStyle, ILineStyle LineStyle,
                                String Caption, Object Tag, Char SecurityDomain, Dataweb.NShape.WinFormsUI.Display Display,
                                Dataweb.NShape.Project Project, Layer Layer)
        {
            Dataweb.NShape.GeneralShapes.Label shape;
            shape = (Dataweb.NShape.GeneralShapes.Label)Project.ShapeTypes["Label"].CreateInstance();
            shape.Height = Height;
            shape.Width = Width;

            shape.X = X;

            shape.Y = Y;

            shape.FillStyle = FillStyle;

            shape.SetCaptionText(0, Caption);

            shape.CharacterStyle = Project.Design.CharacterStyles.Heading3;

            shape.Tag = Tag;

            shape.SecurityDomainName = SecurityDomain;

            Display.Diagram.Shapes.Add(shape, 10);

            Display.Diagram.AddShapeToLayers(shape, Layer.Id);

            Project.Repository.Insert((Shape)shape, Display.Diagram);

            return shape;
        }


        public static Polyline DrawParallelArrow(Polyline Shape, Object Tag, Dataweb.NShape.WinFormsUI.Display Display, Dataweb.NShape.Project Project, ILineStyle LineStyle)
        {
            Polyline parallel = (Polyline)Project.ShapeTypes["Polyline"].CreateInstance();
            parallel.StartCapStyle = Project.Design.CapStyles.OpenArrow;
            parallel.LineStyle = LineStyle;
            parallel.Tag = Tag;
            parallel.SecurityDomainName = 'B';

            Point p0 = Shape.GetControlPointPosition(ControlPointId.FirstVertex);
            Point p1 = Shape.GetControlPointPosition(ControlPointId.LastVertex);

            Point n0 = Shape.CalcNormalVector(p0);
            Point n1 = Shape.CalcNormalVector(p1);

            parallel.MoveControlPointTo(ControlPointId.FirstVertex, n0.X, n0.Y, ResizeModifiers.None);
            parallel.MoveControlPointTo(ControlPointId.LastVertex, n1.X, n1.Y, ResizeModifiers.None);

            Display.Diagram.Shapes.Add(parallel);

            Project.Repository.Insert((Shape)parallel, Display.Diagram);

            return parallel;
        }

        public static Polyline DrawParallelArrow(Polyline Shape, Object Tag, Dataweb.NShape.WinFormsUI.Display Display, Dataweb.NShape.Project Project, ILineStyle LineStyle, Layer Layer)
        {
            Polyline parallel = (Polyline)Project.ShapeTypes["Polyline"].CreateInstance();
            parallel.StartCapStyle = Project.Design.CapStyles.OpenArrow;
            parallel.LineStyle = LineStyle;
            parallel.Tag = Tag;
            parallel.SecurityDomainName = 'B';

            Point p0 = Shape.GetControlPointPosition(ControlPointId.FirstVertex);
            Point p1 = Shape.GetControlPointPosition(ControlPointId.LastVertex);

            Point n0 = Shape.CalcNormalVector(p0);
            Point n1 = Shape.CalcNormalVector(p1);

            parallel.MoveControlPointTo(ControlPointId.FirstVertex, n0.X, n0.Y, ResizeModifiers.None);
            parallel.MoveControlPointTo(ControlPointId.LastVertex, n1.X, n1.Y, ResizeModifiers.None);

            Display.Diagram.Shapes.Add(parallel);

            Display.Diagram.AddShapeToLayers(parallel, Layer.Id);

            Project.Repository.Insert((Shape)parallel, Display.Diagram);

            return parallel;
        }

        public static Polyline ConnectShapes(Shape Start, Shape End, Object Tag, Dataweb.NShape.WinFormsUI.Display Display,
                            Dataweb.NShape.Project Project, ILineStyle LineStyle, Layer Layer)
        {
            Polyline arrow = (Polyline)Project.ShapeTypes["Polyline"].CreateInstance();

            arrow.LineStyle = LineStyle;
            arrow.Tag = Tag;
            arrow.SecurityDomainName = 'B';

            if (Display.InvokeRequired)
            {
                Display.BeginInvoke(new MethodInvoker(() => AddShape(Display, Project, arrow)));
            }
            else
            {
                Display.Diagram.Shapes.Add(arrow);
                Project.Repository.Insert((Shape)arrow, Display.Diagram);
            }

            arrow.Connect(ControlPointId.FirstVertex, Start, ControlPointId.Reference);
            arrow.Connect(ControlPointId.LastVertex, End, ControlPointId.Reference);

            Display.Diagram.AddShapeToLayers(arrow, Layer.Id);


            return arrow;
        }


        private static Polyline ConnectShapesWithCapStyle(Shape Start, Shape End, ICapStyle StartCapStyle, ICapStyle EndCapStyle, Object Tag, Dataweb.NShape.WinFormsUI.Display Display, Dataweb.NShape.Project Project)
        {
            Polyline arrow = (Polyline)Project.ShapeTypes["Polyline"].CreateInstance();
            arrow.Tag = Tag;
            arrow.SecurityDomainName = 'B';
            Display.Diagram.Shapes.Add(arrow);
            arrow.StartCapStyle = StartCapStyle;
            arrow.EndCapStyle = EndCapStyle;
            arrow.Connect(ControlPointId.FirstVertex, Start, ControlPointId.Reference);
            arrow.Connect(ControlPointId.LastVertex, End, ControlPointId.Reference);

            Project.Repository.Insert((Shape)arrow, Display.Diagram);


            return arrow;
        }

        public static CircularArc ConnectShapesArc(Shape Start, Shape End, ICapStyle StartCapStyle, ICapStyle EndCapStyle, Object Tag,
                                            Dataweb.NShape.WinFormsUI.Display Display, Dataweb.NShape.Project Project, ILineStyle LineStyle, Layer Layer)
        {
            CircularArc arc = (CircularArc)Project.ShapeTypes["CircularArc"].CreateInstance();
            arc.LineStyle = LineStyle;
            arc.Tag = Tag;
            arc.SecurityDomainName = 'B';


            Display.Diagram.Shapes.Add(arc);
            arc.StartCapStyle = StartCapStyle;
            arc.EndCapStyle = EndCapStyle;
            arc.Connect(ControlPointId.FirstVertex, Start, ControlPointId.Reference);
            arc.Connect(ControlPointId.LastVertex, End, ControlPointId.Reference);

            Point firstPt = arc.GetControlPointPosition(ControlPointId.FirstVertex);
            Point lastPt = arc.GetControlPointPosition(ControlPointId.LastVertex);
            Point dstPos = Point.Empty;
            dstPos.X = ((firstPt.X + lastPt.X) / 2) + 1;
            dstPos.Y = ((firstPt.Y + lastPt.Y) / 2) + 1;

            arc.InsertVertex(ControlPointId.LastVertex, dstPos.X, dstPos.Y);

            Display.Diagram.AddShapeToLayers(arc, Layer.Id);

            Project.Repository.Insert((Shape)arc, Display.Diagram);

            return arc;
        }

        public static void ClearShapes(Dataweb.NShape.WinFormsUI.Display Display, Layer Layer)
        {

            Display.Project.Repository.DeleteAll(Display.Diagram.Shapes.Where(s => Display.Diagram.Layers.GetLayer(Display.Diagram.GetShapeLayers(s)).Id == Layer.Id));

            Display.Diagram.Shapes.RemoveRange(Display.Diagram.Shapes.Where(s => Display.Diagram.Layers.GetLayer(Display.Diagram.GetShapeLayers(s)).Id == Layer.Id));

        }

        public static void ClearShapes(Dataweb.NShape.WinFormsUI.Display Display)
        {
            Display.Project.Repository.DeleteAll(Display.Diagram.Shapes);

            Display.Diagram.Shapes.RemoveRange(Display.Diagram.Shapes);
        }

        public static Shape FindShapeByTagString(String Tag, Dataweb.NShape.WinFormsUI.Display Display, Layer Layer)
        {
            Shape shape = Display.Diagram.Shapes.Where(s => s.Tag.ToString() == Tag && Display.Diagram.Layers.GetLayer(Display.Diagram.GetShapeLayers(s)).Id == Layer.Id).SingleOrDefault();

            return shape;
        }

        public static Shape FindShapeByShapeTag(display.UI.ShapeTag ShapeTag, string TypeName, Dataweb.NShape.WinFormsUI.Display Display)
        {
            return Display.Diagram.Shapes.Where(s => s!= null && s.Tag != null && s.Tag.GetType() == typeof(display.UI.ShapeTag) && ((display.UI.ShapeTag)s.Tag).Id == ShapeTag.Id && s.Type.Name == TypeName).SingleOrDefault();
        }

        public static List<Shape> FindShapesByTagString(String Tag, Dataweb.NShape.WinFormsUI.Display Display)
        {
            return Display.Diagram.Shapes.Where(s => s.Tag.ToString() == Tag).ToList();
        }

        public static void HighlightShapes(List<Shape> Shapes, ILineStyle LineStyle)
        {

            foreach (Shape shape in Shapes)
            {
                shape.LineStyle = LineStyle;
            }

        }

        public static void HighlightShapeByShapeTag(UI.ShapeTag Tag, Dataweb.NShape.WinFormsUI.Display Display)
        {
            Shape shape = Display.Diagram.Shapes.Where(s => s.Tag != null && s.Tag.GetType() == typeof(UI.ShapeTag) && ((UI.ShapeTag)s.Tag).Id == Tag.Id).SingleOrDefault();

            if (shape != null)
            {
                shape.LineStyle = Display.Project.Design.LineStyles.Highlight;
            }
        }

        public static void HighlightLink(UI.ShapeTag Tag, Dataweb.NShape.WinFormsUI.Display Display)
        {
            Shape shape = Display.Diagram.Shapes.Where(s => s.Type.Name == "Polyline" &&
                                                            s.Tag != null && 
                                                            s.Tag.GetType() == typeof(UI.ShapeTag) && 
                                                            ((UI.ShapeTag)s.Tag).Id == Tag.Id).SingleOrDefault();

            if (shape != null)
            {
                shape.LineStyle = Display.Project.Design.LineStyles.Highlight;
                ((Polyline)shape).StartCapStyle = Display.Project.Design.CapStyles.OpenArrow;
            }
        }

        public static void HighlightShapeByTagString(String Tag, Dataweb.NShape.WinFormsUI.Display Display, ILineStyle LineStyle, Layer Layer)
        {
            Shape shape = Display.Diagram.Shapes.Where(s => s.Tag.ToString() == Tag && Display.Diagram.Layers.GetLayer(Display.Diagram.GetShapeLayers(s)).Id == Layer.Id).SingleOrDefault();

            shape.LineStyle = LineStyle;
        }

        public static void DimUnhighlightedShapes(Dataweb.NShape.WinFormsUI.Display Display, ILineStyle HighlightLineStyle, ILineStyle DimLineStyle, IFillStyle DimFillStyle, Layer Layer)
        {
            List<Shape> shapes = Display.Diagram.Shapes.Where(s => s.LineStyle != HighlightLineStyle && Display.Diagram.Layers.GetLayer(Display.Diagram.GetShapeLayers(s)).Id == Layer.Id).ToList();

            foreach (Shape shape in shapes)
            {
                shape.LineStyle = DimLineStyle;

                if (shape.Type.Name == "Circle")
                {
                    ((Circle)shape).FillStyle = DimFillStyle;
                }
            }
        }

        public static void HighlightShapesByTagString(String Tag, Dataweb.NShape.WinFormsUI.Display Display, ILineStyle LineStyle, Layer Layer)
        {
            List<Shape> shapes = Display.Diagram.Shapes.Where(s => s.Tag.ToString() == Tag && Display.Diagram.Layers.GetLayer(Display.Diagram.GetShapeLayers(s)).Id == Layer.Id).ToList();

            foreach (Shape shape in shapes)
            {
                shape.LineStyle = LineStyle;
            }
        }

        public static void SetShapesLineStyleByTagStringAndShapeType(String Tag, Dataweb.NShape.WinFormsUI.Display Display, ILineStyle LineStyle, ShapeType ShapeType)
        {
            List<Shape> shapes = Display.Diagram.Shapes.Where(s => s.Tag.ToString() == Tag && s.Type == ShapeType).ToList();

            foreach (Shape shape in shapes)
            {

                shape.LineStyle = LineStyle;

            }

        }

        public static void SetCircleFillStyleByTagString(String Tag, Dataweb.NShape.WinFormsUI.Display Display, IFillStyle FillStyle, Project Project)
        {
            List<Shape> shapes = Display.Diagram.Shapes.Where(s => s.Tag.ToString().Contains(Tag) && s.Type == Project.ShapeTypes["Circle"]).ToList();

            foreach (Shape shape in shapes)
            {

                ((Circle)shape).FillStyle = FillStyle;

            }

        }

        public static void SetAllCirclesFillStyle(Dataweb.NShape.WinFormsUI.Display Display, IFillStyle FillStyle, Project Project)
        {
            List<Shape> shapes = Display.Diagram.Shapes.Where(s => s.Type == Project.ShapeTypes["Circle"]).ToList();

            foreach (Shape shape in shapes)
            {

                ((Circle)shape).FillStyle = FillStyle;

            }

        }

        public static void SetAllShapesLineStyle(Dataweb.NShape.WinFormsUI.Display Display, ILineStyle SourceLineStyle, ILineStyle TargetLineStyle)
        {
            foreach (Shape shape in Display.Diagram.Shapes)
            {
                if (shape.LineStyle == SourceLineStyle)
                {
                    shape.LineStyle = TargetLineStyle;
                }
            }
        }

        public static void AddShapesToLayerByShapeType(Dataweb.NShape.WinFormsUI.Display Display, ShapeType ShapeType, Layer Layer)
        {
            List<Shape> shapes = Display.Diagram.Shapes.Where(s => s.Type == ShapeType).ToList();


            Display.Diagram.AddShapesToLayers(shapes, Layer.Id);

        }

        public static void RemoveShapesFromLayerByShapeType(Dataweb.NShape.WinFormsUI.Display Display, ShapeType ShapeType, Layer Layer)
        {
            List<Shape> shapes = Display.Diagram.Shapes.Where(s => s.Type == ShapeType).ToList();


            Display.Diagram.RemoveShapesFromLayers(shapes, Layer.Id);

        }

        public static void LayouterTopology(int LayerDistance, int RowDistance, FlowLayouter.FlowDirection Direction, int SpringRate, int Repulsion, int RepulsionRange, int Friction, int Mass, Boolean Fit,
                              int FitX0, int FitY0, int FitX1, int FitY1, Dataweb.NShape.WinFormsUI.Display Display, int Timeout, LayouterType Type, Dataweb.NShape.Project Project)
        {

            switch (Type)
            {
                case LayouterType.Expansion:
                    break;
                case LayouterType.Flow:
                    FlowLayouter flowLayouter = new FlowLayouter(Project);
                    flowLayouter.Direction = Direction;
                    flowLayouter.LayerDistance = LayerDistance;
                    flowLayouter.RowDistance = RowDistance;
                    flowLayouter.AllShapes = Display.Diagram.Shapes;
                    flowLayouter.Shapes = Display.Diagram.Shapes;

                    flowLayouter.Prepare();
                    flowLayouter.Execute(Timeout);

                    if (Fit) { flowLayouter.Fit(FitX0, FitY0, FitX1, FitY1); }

                    break;
                case LayouterType.Grid:
                    break;
                case LayouterType.Repulsion:
                    RepulsionLayouter repulsionLayouter = new RepulsionLayouter(Project);
                    repulsionLayouter.SpringRate = SpringRate; //8
                    repulsionLayouter.Repulsion = Repulsion; //3
                    repulsionLayouter.RepulsionRange = RepulsionRange; //600
                    repulsionLayouter.Friction = Friction; //0
                    repulsionLayouter.Mass = Mass; //50
                    repulsionLayouter.AllShapes = Display.Diagram.Shapes;
                    repulsionLayouter.Shapes = Display.Diagram.Shapes;

                    repulsionLayouter.Prepare();

                    repulsionLayouter.Execute(Timeout);

                    if (Fit) { repulsionLayouter.Fit(FitX0, FitY0, FitX1, FitY1); }

                    break;
                default:
                    break;
            }

        }
    }
}
