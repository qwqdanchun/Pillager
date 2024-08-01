using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;

namespace Pillager.Helper
{
    public abstract class ICommand
    {
        public abstract void Save(string path);
    }
}
