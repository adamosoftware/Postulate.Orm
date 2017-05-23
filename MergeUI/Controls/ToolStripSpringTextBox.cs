using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Postulate.MergeUI.Controls
{
    // thanks to https://msdn.microsoft.com/en-us/library/ms404304(v=vs.110).aspx

    public class ToolStripSpringTextBox : ToolStripTextBox
    {
        public override Size GetPreferredSize(Size constrainingSize)
        {
            if (IsOnOverflow || Owner.Orientation == Orientation.Vertical)
            {
                return DefaultSize;
            }

            int width = Owner.DisplayRectangle.Width;
            if (Owner.OverflowButton.Visible)
            {
                width = width - Owner.OverflowButton.Width - Owner.OverflowButton.Margin.Horizontal;
            }

            int springBoxCount = 0;
            foreach (ToolStripItem item in Owner.Items)
            {                
                if (item.IsOnOverflow) continue;

                if (item is ToolStripSpringTextBox)
                {                    
                    springBoxCount++;
                    width -= item.Margin.Horizontal;
                }
                else
                {
                    width = width - item.Width - item.Margin.Horizontal;
                }
            }

            if (springBoxCount > 1) width /= springBoxCount;

            if (width < DefaultSize.Width) width = DefaultSize.Width;

            Size size = base.GetPreferredSize(constrainingSize);
            size.Width = width;
            return size;
        }
    }
}
