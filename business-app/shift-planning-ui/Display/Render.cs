using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

#region NShape
using Dataweb;
using Dataweb.NShape;
using Dataweb.NShape.Advanced;
using Dataweb.NShape.Layouters;
using Dataweb.NShape.GeneralShapes;
#endregion

#region Neo4J
using Neo4j.Driver.V1;
#endregion


namespace shift.ui.display
{
    class Render
    {

        public static Shape RenderNode(yggdrasil2.Topology.Node.Node Node, Dataweb.NShape.WinFormsUI.Display Display)
        {
            //Globals
            Project project = Display.Project;
            Diagram diagram = Display.Diagram;
            Design design = project.Design;

            Layer layer = diagram.Layers["NETWORK"];
            //

            //Defaults
            Point position = new Point(diagram.Width / 2, diagram.Height / 2);
            int diameter = 100;
            IFillStyle fill = design.FillStyles.Transparent;
            ILineStyle stroke = design.LineStyles.Normal;
            string caption = "";
            //

            //Node Labels
            if (!Node.IsPseudonode)
            {
                diameter = 600;
                fill = design.FillStyles.Blue;
                stroke = design.LineStyles.None;

                caption = Node.NodeName;
            }
            else
            {
                diameter = 60;
                fill = design.FillStyles.Black;
                stroke = design.LineStyles.None;
            }
            //


            if(!Node.OperationalStatus)
            {
                fill = design.FillStyles.Red;
            }

            Shape exists = NShapeHelper.FindShapeByShapeTag(new display.UI.ShapeTag(Node.Id.As<string>(), null), "Circle", Display);

            if (exists != null)
            {

                try
                {
                    //Update Node -- TODO: Move this somewhere else
                    ((Circle)exists).Diameter = diameter;
                    ((Circle)exists).FillStyle = fill;
                    ((Circle)exists).LineStyle = stroke;
                    ((display.UI.ShapeTag)exists.Tag).LineStyle = stroke;
                    //
                }
                catch (Exception)
                {
                    System.Threading.Thread.Sleep(1);
                    RenderNode(Node, Display);
                }

                return exists;
            }
            else
            {
                return NShapeHelper.DrawCircle(diameter, position.X, position.Y, fill, stroke, caption, new display.UI.ShapeTag(Node.Id.As<string>(), Node,
                    stroke, fill, Display.Project.Design.CapStyles.None), 'B', Display, project, layer);
            }
        }

        public static Shape RenderNode(Neo4j.Driver.V1.INode Node, Dataweb.NShape.WinFormsUI.Display Display)
        {
            //Globals
            Project project = Display.Project;
            Diagram diagram = Display.Diagram;
            Design design = project.Design;

            Layer layer = diagram.Layers["NETWORK"];
            //

            //Defaults
            Point position = new Point(diagram.Width / 2, diagram.Height / 2);
            int diameter = 100;
            IFillStyle fill = design.FillStyles.Transparent;
            ILineStyle stroke = design.LineStyles.Normal;
            string caption = "";
            //

            //Node Labels
            if (Node.Labels.Contains("Node"))
            {
                diameter = 600;
                fill = design.FillStyles.Blue;
                stroke = design.LineStyles.None;

                caption = Node.Properties["Node_Name"].As<string>();
            }
            else if (Node.Labels.Contains("Pseudonode"))
            {
                diameter = 60;
                fill = design.FillStyles.Black;
                stroke = design.LineStyles.None;
            }
            //


            Shape exists = NShapeHelper.FindShapeByShapeTag(new display.UI.ShapeTag(Node.Id.As<string>(), null,
                stroke, fill, Display.Project.Design.CapStyles.None), "Circle", Display);

            if (exists != null)
            {

                //Update Node -- TODO: Move this somewhere else
                ((Circle)exists).Diameter = diameter;
                ((Circle)exists).FillStyle = fill;
                exists.LineStyle = stroke;
                //

                return exists;
            }
            else
            {
                return NShapeHelper.DrawCircle(diameter, position.X, position.Y, fill, stroke, caption, new display.UI.ShapeTag(Node.Id.As<string>(), Node,
                    stroke, fill, Display.Project.Design.CapStyles.None), 'B', Display, project, layer);
            }
        }

        public static Shape RenderRelationship(yggdrasil2.Topology.IGP.Link.Link link, Dataweb.NShape.WinFormsUI.Display Display)
        {
            //Globals
            Project project = Display.Project;
            Diagram diagram = Display.Diagram;
            Design design = project.Design;

            Layer layer = diagram.Layers["NETWORK"];
            //

            //Defaults
            ILineStyle stroke = design.LineStyles.Normal;
            ICapStyle startCap = design.CapStyles.None;
            ICapStyle endCap = design.CapStyles.None;
            //

            if (link.OperationalStatus)
            {
                stroke = design.LineStyles.Green;
            }
            else
            {
                stroke = design.LineStyles.Red;
            }

            //Start and End Shapes
            Shape shapeStart = NShapeHelper.FindShapeByShapeTag(new display.UI.ShapeTag(link.SourceNode, null), "Circle", Display);
            Shape shapeEnd = NShapeHelper.FindShapeByShapeTag(new display.UI.ShapeTag(link.TargetNode, null), "Circle", Display);

            while (shapeStart == null || shapeEnd == null)
            {
                System.Threading.Thread.Sleep(1);

                shapeStart = NShapeHelper.FindShapeByShapeTag(new display.UI.ShapeTag(link.SourceNode, null), "Circle", Display);
                shapeEnd = NShapeHelper.FindShapeByShapeTag(new display.UI.ShapeTag(link.TargetNode, null), "Circle", Display);
            }

            Shape exists = NShapeHelper.FindShapeByShapeTag(new display.UI.ShapeTag(link.Id, null), "Polyline", Display);

            Shape reverseExists = NShapeHelper.FindShapeByShapeTag(new display.UI.ShapeTag(link.ReverseId, null), "Polyline", Display);

            if (exists != null)
            {
                exists.LineStyle = stroke;
                ((UI.ShapeTag)exists.Tag).LineStyle = stroke;

                if (reverseExists != null)
                {
                    reverseExists.LineStyle = stroke;
                    ((UI.ShapeTag)reverseExists.Tag).LineStyle = stroke;
                }
                
                return exists;
            }
            else
            {
                return NShapeHelper.ConnectShapes(shapeStart, shapeEnd,
                    new display.UI.ShapeTag(link.Id, link, stroke, null, Display.Project.Design.CapStyles.None), 
                    Display, project, stroke, layer);
            }
        }

        public static Shape RenderRelationship(Neo4j.Driver.V1.IRelationship Rel, Dataweb.NShape.WinFormsUI.Display Display)
        {
            //Globals
            Project project = Display.Project;
            Diagram diagram = Display.Diagram;
            Design design = project.Design;

            Layer layer = diagram.Layers["NETWORK"];
            //

            //Defaults
            ILineStyle stroke = design.LineStyles.Normal;
            ICapStyle startCap = design.CapStyles.None;
            ICapStyle endCap = design.CapStyles.None;
            //

            if (Rel.Properties.ContainsKey("Operational_Status"))
            {
                if (Rel.Properties["Operational_Status"].As<bool>())
                {
                    stroke = design.LineStyles.Green;
                }
                else
                {
                    stroke = design.LineStyles.Red;
                }
            }


            //Start and End Shapes
            Shape shapeStart = NShapeHelper.FindShapeByShapeTag(new display.UI.ShapeTag(Rel.StartNodeId.As<string>(), null), "Circle", Display);
            Shape shapeEnd = NShapeHelper.FindShapeByShapeTag(new display.UI.ShapeTag(Rel.EndNodeId.As<string>(), null), "Circle", Display);


            Shape exists = NShapeHelper.FindShapeByShapeTag(new display.UI.ShapeTag(Rel.Id.As<string>(), null), "Polyline", Display);

            if (exists != null)
            {
                exists.LineStyle = stroke;

                return exists;
            }
            else
            {

                //Rel Type
                switch (Rel.Type)
                {
                    case "Link":
                        return NShapeHelper.ConnectShapes(shapeStart, shapeEnd,
                            new display.UI.ShapeTag(Rel.Id.As<string>(), Rel, stroke, null, Display.Project.Design.CapStyles.None),
                            Display, project, stroke, layer);
                    default:
                        return NShapeHelper.ConnectShapes(shapeStart, shapeEnd,
                            new display.UI.ShapeTag(Rel.Id.As<string>(), Rel, stroke, null, Display.Project.Design.CapStyles.None),
                            Display, project, stroke, layer);
                }
                //
            }
        }

        public static Shape RenderRelationshipVector(Neo4j.Driver.V1.IRelationship Rel, Dataweb.NShape.WinFormsUI.Display Display)
        {
            //Globals
            Project project = Display.Project;
            Diagram diagram = Display.Diagram;
            Design design = project.Design;

            Layer layer = diagram.Layers["OVERLAY"];
            //

            //Defaults
            ILineStyle stroke = design.LineStyles.Green;
            ICapStyle startCap = design.CapStyles.None;
            ICapStyle endCap = design.CapStyles.OpenArrow;
            //


            //Reference Shape
            Shape shapeReference = NShapeHelper.FindShapeByShapeTag(new display.UI.ShapeTag(Rel.Id.As<string>(), Display.Project.Design.CapStyles.None), "Polyline", Display);

            return NShapeHelper.DrawParallelArrow((Polyline)shapeReference, Rel, Display, project, stroke, layer);

        }

        public static void RestoreLineStyles(Dataweb.NShape.WinFormsUI.Display Display)
        {
            foreach (var shape in Display.Diagram.Shapes)
            {
                if(shape.Tag != null && shape.Tag.GetType() == typeof(UI.ShapeTag))
                {
                    shape.LineStyle = ((UI.ShapeTag)shape.Tag).LineStyle;

                    if(shape.Type.Name == "Polyline")
                    {
                        ((Polyline)shape).EndCapStyle = ((UI.ShapeTag)shape.Tag).CapStyle;
                        ((Polyline)shape).StartCapStyle = ((UI.ShapeTag)shape.Tag).CapStyle;
                    }
                }
            }
        }

        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp, bool toLocalTimeZone)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp);

            if (toLocalTimeZone)
            {
                return TimeZone.CurrentTimeZone.ToLocalTime(dtDateTime);
            }
            else
            {
                return dtDateTime;
            }
        }
    }
}
