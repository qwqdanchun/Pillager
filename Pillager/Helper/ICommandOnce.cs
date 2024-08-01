using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;

namespace Pillager.Helper
{
    public abstract class ICommandOnce
    {
        public abstract void Save(string path);
    }
}
