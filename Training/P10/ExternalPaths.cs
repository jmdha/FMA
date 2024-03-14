using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tools;

namespace P10
{
    public static class ExternalPaths
    {
        public static string FastDownwardPath = PathHelper.RootPath("../Dependencies/fast-downward/fast-downward.py");
        public static string StackelbergPath = PathHelper.RootPath("../Dependencies/stackelberg-planner/src/fast-downward.py");
    }
}
