using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Codist;
public static class Extensions {
  public static Brush MakeFrozen(this Brush brush) {
    brush.Freeze();
    return brush;
  }
  public static Pen MakeFrozen(this Pen pen) {
    pen.Freeze();
    return pen;
  }
}
