using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MIPCXConsole
{
    public class Entities
    {
        public class StructTrace
        {
            public int Id { get; set; }
            public double Probab { get; set; }
            public string Trace { get; set; }
            public List<string> TransitionsList = new List<string>();
            public List<Action> ActionSynchList { get; set; }
            public List<Action> ActionAsynchList { get; set; }
            public List<Action> ActionsList { get; set; }
            public List<State> StatesList = new List<State>();
            public List<State> FInalStatesList = new List<State>();
        }

        public class State
        {
            public int Id { get; set; }
            public string value { get; set; }
            public List<string> variablesList = new List<string>();
            public List<string> valuesList = new List<string>();
        }
        //public class ActionSynch
        //{
        //    public string Name { get; set; }
        //    public List<Module> ModulesList = new List<Module>();
        //}
        //public class ActionAsynch
        //{
        //    public string Name { get; set; }
        //    public string Module { get; set; }
        //}

        public class Action
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Value { get; set; }
            public State Previous_state { get; set; }
            public State Next_state { get; set; }
            public List<string> List_Variables { get; set; }
            public List<string> List_Values { get; set; }
        }

        //public class Module
        //{
        //    public string Name { get; set; }
        //    public List<ActionSynch> ActionSynchList = new List<ActionSynch>();
        //    public List<ActionAsynch> ActionAsynchList = new List<ActionAsynch>();
        //}

    }

}
