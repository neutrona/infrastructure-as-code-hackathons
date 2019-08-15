using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dataweb.NShape;

namespace shift.ui.display
{
    class UI
    {
        public class ShapeTag
        {
            public string Id { get; set; }
            public object Object { get; set; }
            public ILineStyle LineStyle { get; set; }
            public IFillStyle FillStyle { get; set; }
            public ICapStyle CapStyle { get; set; }

            public ShapeTag(string Id, object Object,
                ILineStyle LineStyle, IFillStyle FillStyle, ICapStyle CapStyle)
            {
                this.Id = Id;
                this.Object = Object;
                this.LineStyle = LineStyle;
                this.FillStyle = FillStyle;
                this.CapStyle = CapStyle;
            }

            public ShapeTag(string Id, object Object)
            {
                this.Id = Id;
                this.Object = Object;
            }
        }
    }
}
