using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TriEngineEditor.Utilities.Controls
{
    internal class ScalarBox : NumberBox
    {
        static ScalarBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ScalarBox), new FrameworkPropertyMetadata(typeof(ScalarBox)));
        }
    }
}
