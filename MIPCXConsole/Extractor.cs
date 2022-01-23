using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Z3;

namespace MIPCXConsole
{
    public class Extractor
    {

        public static List<Entities.StructTrace> structTracesList = new List<Entities.StructTrace>();

        //public static List<Entities.ActionSynch> LstOfWinActionSynch = new List<Entities.ActionSynch>();
        //public static List<Entities.ActionAsynch> LstOfWinActionASynch = new List<Entities.ActionAsynch>();
        //public static List<Entities.ActionSynch> lstOfSynchActions = new List<Entities.ActionSynch>();
        public static List<Entities.Action> lstOfActions = new List<Entities.Action>();

        public static List<Entities.State> statesListAll = new List<Entities.State>();
        public static List<Entities.State> lstOfGoalStates = new List<Entities.State>();

/// <summary>
        /// This is a constructor, in which we upload the MIPCX
        /// and extract all debuggging related information, such as traces, actions, variables, ...
        /// </summary>
        public Extractor()
        {
            var MipcxFileName = XElement.Load("zeroconf.nm.sol.traces_ALL_167_175_229.xml");
            Console.WriteLine("counterexample loaded");
            var traces = MipcxFileName.Descendants("DiagnosticPath");
            int index = 0;
            foreach (var trace in traces)
            {
                Entities.StructTrace structTrace = new Entities.StructTrace();
                //structTrace.Probab = trace.FirstNode(x => (string)x.Element("Trace").Value);
                //structTrace.Probab = Convert.ToDouble(trace.Element("Probability").Value.ToString());
                structTrace.Probab = 1;
                structTrace.Trace = trace.Element("Trace").Value.ToString();
                structTrace.Id = index;
                index++;
                structTracesList.Add(structTrace);
            }

            //traces.Select(x => (string)x.Element("Trace").Value);
            List<string> tabOfTrans;
            //string[] lstOfTrans = { };
            List<Entities.Action> lstOfActionsSynchByTrace = new List<Entities.Action>();
            List<Entities.Action> lstOfActionsASynchByTrace = new List<Entities.Action>();
            List<Entities.Action> lstOfActionsByTrace = new List<Entities.Action>();
            // short path low prob

            Console.WriteLine("length MIPCX:" + traces.Count());

            foreach (var trace in structTracesList)
            {

                tabOfTrans = new List<string>();
                lstOfActionsSynchByTrace = new List<Entities.Action>();
                lstOfActionsByTrace = new List<Entities.Action>();

                var copyTrace1 = trace;
                var copyTrace2 = trace;
                //Console.WriteLine("Proba=" + copyTrace1.Probab);
                //------------- States -----------
                string statePattern = "\\{(.*?)\\}";
                string replacedText = Regex.Replace(copyTrace1.Trace, statePattern, "");
                var lstOfStatesByTrace = Regex.Split(replacedText, @"\n|\n\n").Where(s => s != "");
                var lstOfStatesFinal = lstOfStatesByTrace.Where(s => s != "");


                foreach (var s in lstOfStatesByTrace)
                {
                    Entities.State st = new Entities.State();
                    st.value = s;
                    trace.StatesList.Add(st);
                }

                foreach (var s in lstOfStatesFinal)
                {
                    Entities.State st = new Entities.State();
                    st.value = s;
                    trace.FInalStatesList.Add(st);
                }

                ////////// Extract states, their variables and values
                //trace.StatesList = lstOfStatesFinal.ToList();

                foreach (var state in trace.StatesList)
                {
                    if (!statesListAll.Any(s => s.value == state.value))
                    {
                        Entities.State st = new Entities.State();
                        st.value = state.value;
                        st.Id = (statesListAll.Count - 1) + 1;
                        statesListAll.Add(st);


                        var last = trace.StatesList[trace.StatesList.Count - 1];
                        if (lstOfGoalStates.All(s => s != last))
                        {
                            lstOfGoalStates.Add(last);
                        }

                        ///// variables
                        List<string> lstOfVariablesByState = new List<string>();
                        string[] words = state.value.Split('=');

                        foreach (var word in words)
                        {
                            string v = word.Split(',').LastOrDefault();
                            lstOfVariablesByState.Add(v);
                        }

                        state.variablesList = lstOfVariablesByState;
                      
                        // Values

                        List<string> lstOfValues = new List<string>();
                        string[] wordsVal = state.value.Split(',');

                        foreach (var word in wordsVal)
                        {
                            string v = word.Split('=').LastOrDefault();
                            lstOfValues.Add(v);
                        }

                        state.valuesList = lstOfValues;
                    }
                }


                //------------- Transitions -----------
                Regex regex = new Regex(@"\{>.*?\>}");
                MatchCollection matchesTransitions = regex.Matches(copyTrace2.Trace);
                Regex regexSynchMove = new Regex(@"(?<=\[).*?(?=\])"); // get move if synch 
                Regex regexTrans = new Regex(@"(?<=\>).*?(?=\})"); // get transition
                var patternTransition = @"(?<=\<).*?(?=\>)";
                Regex regexModuleNonSynch = new Regex(@"(?<=\{>).*?(?=\>)"); // non synch Module
                Regex regexModuleSynch = new Regex(@"(?<=\-).*?(?=\-)"); // sync Modules
                for (int i = 0; i < matchesTransitions.Count; i++)
                {
                    // synch actions
                    //MatchCollection matchSynchAct = regexSynchMove.Matches(matchesTransitions[i].ToString());
                    // on synch actions
                    MatchCollection matchregexTrans = regexTrans.Matches(matchesTransitions[i].ToString());

                    if (matchesTransitions[i].ToString() != String.Empty)
                        tabOfTrans.Add(matchesTransitions[i].Value); // List of complete transitions

                    MatchCollection matchregexModuleNonSynch =
                        regexModuleNonSynch.Matches(matchesTransitions[i].ToString());
                    Entities.Action action = new Entities.Action();
                    string trans = matchregexTrans[0].ToString().Split('<', '>')[4];
                    // calculate prob of trace
                    trace.Probab = trace.Probab * Convert.ToDouble(matchregexTrans[0].ToString().Split('<', '>')[3]);
                    // Fill all action info
                    action.Id = i;
                    action.Value = trans;
                    action.Name = matchregexTrans[0].ToString().Split('<', '>')[1];
                    action.Previous_state = trace.StatesList[i];
                    action.Next_state = trace.StatesList[i + 1];
                    lstOfActionsByTrace.Add(action);

                    Regex regexVar = new Regex(@"(?<=\=).*?(?=\})");
                    MatchCollection matchesVariables = regexVar.Matches(trans);

                    action.List_Variables = trans.Split('\'').ToList();
                }


                trace.ActionSynchList = lstOfActionsSynchByTrace;
                trace.ActionAsynchList = lstOfActionsASynchByTrace;
                trace.ActionsList = lstOfActionsByTrace;
                //trace.StatesList = lstOfStatesByTrace.ToList();
                trace.TransitionsList = tabOfTrans.ToList();

                // making distinct lists of synch and asynch actions
                foreach (var action in trace.ActionsList)
                    //foreach (var action in trace.ActionsList)
                {
                    // Actions constraint when synch actions
                    if (action.Name != "[]")
                    {
                        trace.ActionSynchList.Add(action);
                    }
                    else
                    {
                        trace.ActionAsynchList.Add(action);
                    }
                }
            }
        }
/// <summary>
/// Sythnesis function for generrating a list of constraints to the user 
/// </summary>
        public void synthesisConstraints()
        {
            double probMax = 100;
            Entities.StructTrace shortTrace = null;
            foreach (var trace in structTracesList)
            {
                if (trace.Probab < probMax)
                {
                    probMax = trace.Probab;
                    shortTrace = trace;
                }
            }

            Console.WriteLine("trace: " + shortTrace.Trace);
            Console.WriteLine("trace lower prob: " + shortTrace.Probab);

            // This part represents constraints syhtnesis from traces of MIPCX
            List<string> lstOfConstraints = new List<string>();
            List<string> lstOfConstraints_Variables = new List<string>();
            List<string> lstOfConstraints_Values = new List<string>();
            List<string> lstOfConstraints_Action = new List<string>();
            List<string> lstOfConstraints_PrePost = new List<string>();
            // In case we need only constraints of shortTrace we iterate through shortTrace only
            // this for all traces
            //foreach (var trace in structTracesList)
            //{
            foreach (var action in shortTrace.ActionsList)
                //foreach (var action in trace.ActionsList)
            {
                // Actions constraint when synch actions
                if (action.Name != "[]")
                {
                    //// variables constraints
                    string cntrntA = "act:" + action.Name + "&" + action.Value;
                    if (!lstOfConstraints_Action.Contains(cntrntA))
                    {
                        lstOfConstraints.Add(cntrntA); //this line concerns synch actions only
                        lstOfConstraints_Action.Add(cntrntA);
                    }
  
                    //// values constraints
                    //lstOfConstraints.Add(action.Value);
                    //pre/post constraints
                    //lstOfConstraints_Action.Add(action.Value + "->" + "Prev :" + action.Previous_state + "Post:" +
                    //                     action.Next_state);
                    //lstOfConstraints.Add(action.Value + "->" + "Prev :" + action.Previous_state + "Post:" +
                    //                            action.Next_state);

                }
                // For non-synch actions


                //// values constraints
                //lstOfConstraints.Add(action.Value);

                //pre/post constraints
                string cntrntP = action.Value + "->" + "Prev :" + action.Previous_state.value +
                               "Post:" +
                               action.Next_state.value;
                if (!lstOfConstraints_PrePost.Contains(cntrntP))
                {
                    lstOfConstraints_PrePost.Add(cntrntP);

                    lstOfConstraints.Add(cntrntP);
                }

                // Variables constraints
                List<string> lstOfVariables = new List<string>();
                string[] words = action.Value.Split('\'');

                foreach (var word in words)
                {
                    string v = word.Split(',').LastOrDefault();
                    lstOfVariables.Add(v);
                }

                action.List_Variables = lstOfVariables;
                string constraintV = "";
                for (int i = 0; i < lstOfVariables.Count-1; i++)
                {
                    constraintV = constraintV + lstOfVariables[i] + "&";
                }

                if (!lstOfConstraints_Variables.Contains(constraintV))
                {
                    lstOfConstraints_Variables.Add(constraintV);
                    lstOfConstraints.Add(constraintV);
                }
                // Values constraints

                List<string> lstOfValues = new List<string>();
                string[] wordsVal = action.Value.Split(',');

                foreach (var word in wordsVal)
                {
                    string v = word.Split('=').LastOrDefault();
                    lstOfValues.Add(v);
                }

                action.List_Values = lstOfValues;
                string constraintVal = "";
                for (int i = 0; i < lstOfValues.Count-1; i++)
                {
                    constraintVal = constraintVal + lstOfVariables[i] + "=" + lstOfValues[i];
                    if (i < lstOfValues.Count - 1)                    
                        constraintVal = constraintVal + "&";
                }

                if (!lstOfConstraints_Values.Contains(constraintVal))
                {
                    lstOfConstraints_Values.Add(constraintVal);
                    lstOfConstraints.Add(constraintVal);
                }
            }
            Console.WriteLine("****************list of Suggested constraints: *********************");
            Console.WriteLine("------Constraints variables------: ");
            lstOfConstraints_Variables.ForEach(c=> Console.WriteLine(c));

            Console.WriteLine("------Constraints values------: ");
            lstOfConstraints_Values.ForEach(c => Console.WriteLine(c));

            Console.WriteLine("------Constraints actions------: ");
            lstOfConstraints_Action.ForEach(c => Console.WriteLine(c));
            // it is better to be interactive witht the user, it only returns when the user asked for a specific variable
            //Console.WriteLine("Constraints Pre/Post: ");
            //lstOfConstraints_PrePost.ForEach(c => Console.WriteLine(c));
            Console.WriteLine("****************list of Suggested constraints: *********************");

        }

        /// <summary>
        /// Influence graph generation function 
        /// </summary>
        public void generateInfluenceGraph()
        { 
            List<string> influenceVarList= new List<string>();
            foreach (var trace in structTracesList)
            {
                // gets the last action containing variables 'l' and 'ip' a in order to find all previous variables having influence on them
                var lastAction =
                    trace.ActionsList.LastOrDefault(a => (a.List_Variables.Contains("l") || a.List_Variables.Contains("ip")));
                if (lastAction != null)
                {
                    int indexlastAction = lastAction.Id;

                    for (int i = 0; i < indexlastAction; i++)
                    {
                        foreach (var VARIAb in trace.ActionsList[i].List_Variables)
                        {
                            if (!influenceVarList.Contains(VARIAb))
                                influenceVarList.Add(VARIAb); // adding distinct influence variables
                        }
                    }
                }
            }
        }

/// <summary>
        /// Check constraints function
        /// </summary>
        /// <param name="lstConstraints"></param>
        public void checKConstraint(List<string> lstConstraints)
        {
            Console.WriteLine("**************** Checking constraints: *********************");

            int numb_trace = 0;
            foreach (var cntrnt in lstConstraints)
            {
                foreach (var trace in structTracesList)
                {
                    //numb_trace += 1;
                    //Console.WriteLine("trace number:" + numb_trace);
                    foreach (var action in trace.ActionsList)
                    {
                        // check if this action contains variables in the given constraint
                        if (action.List_Variables.Any(v => cntrnt.Contains(v)))
                        {
                            if (cntrnt.Contains(action.Name))
                            {
                                using (Context ctx = new Context())
                                {
                                    /********************** First constraint*************/
                                    //UnsatCoreAndProofExample(ctx);
                                    BoolExpr rec = ctx.MkBoolConst(action.Name); // which is rec

                                    var indexVarL = action.List_Variables.FindIndex(v => v == " l");
                                    if (indexVarL != -1)
                                    {
                                        if (action.List_Values != null)
                                        {
                                            var valueOfL = Int16.Parse(action.List_Values[indexVarL]);

                                            //Expr l = ctx.MkConst("l", ctx.MkIntSort());
                                            Expr one = ctx.MkNumeral(1, ctx.MkIntSort());
                                            Expr actulValueOfL = ctx.MkNumeral(valueOfL, ctx.MkIntSort());


                                            //BoolExpr lValue = ctx.MkEq(l, actulValueOfL);
                                            Solver s = ctx.MkSolver();
                                            s.Assert(ctx.MkNot(ctx.MkEq(actulValueOfL, one))); // 0<x

                                            Status result = s.Check();
                                            if (result == Status.UNSATISFIABLE)
                                            {
                                                Console.WriteLine(result);
                                                Console.WriteLine("constraint -  " + cntrnt +
                                                                  "- violated at \n action number: " + action.Id +
                                                                  "\n name: " +
                                                                  action.Name + "\n value: " + action.Value );
                                            }
                                            else if (result == Status.SATISFIABLE)
                                            {
                                                Console.WriteLine("constraint - " + cntrnt + " constraint status : " + result + " on " + action.Value);
                                            }
                                            else
                                            {
                                                Console.WriteLine("Uknown sttaus : " + result);
                                            }
                                        }

                                    }
                                    /********************** First constraint*************/

                                    /********************** Second constraint*************/

                                    var indexVarB = action.List_Variables.FindIndex(v => v == "b");
                                    if (indexVarB != -1)
                                    {
                                        if (action.List_Values != null)
                                        {
                                            var valueOfB = Int16.Parse(action.List_Values[indexVarB]);

                                            var indexVarBPrevious =
                                                action.Previous_state.variablesList.FindIndex(v => v == " b");
                                            var valueOfBPrevious =
                                                Int16.Parse(action.Previous_state.valuesList[indexVarBPrevious]);
                                            //Expr l = ctx.MkConst("l", ctx.MkIntSort());
                                            Expr bPrevious = ctx.MkNumeral(valueOfBPrevious, ctx.MkIntSort());
                                            Expr actulValueOfB = ctx.MkNumeral(valueOfB, ctx.MkIntSort());


                                            //BoolExpr lValue = ctx.MkEq(l, actulValueOfL);
                                            Solver s = ctx.MkSolver();
                                            s.Assert(ctx.MkNot(ctx.MkEq(actulValueOfB, bPrevious))); // 0<x

                                            Status result = s.Check();
                                            if (result == Status.UNSATISFIABLE)
                                            {
                                                Console.WriteLine(result);
                                                Console.WriteLine("constraint -  " + cntrnt +
                                                                  "- violated at \n action number: " + action.Id +
                                                                  "\n name: " +
                                                                  action.Name + "\n value: " + action.Value +
                                                                  "\n Pre: " +
                                                                  action.Previous_state.value + "\n Post: " +
                                                                  action.Next_state.value);
                                            }
                                            else if (result == Status.SATISFIABLE)
                                            {
                                                Console.WriteLine("constraint - " + cntrnt + " constraint status : " + result + " on " + action.Value);
                                            }
                                            else
                                            {
                                                Console.WriteLine("Uknown sttaus : " + result);
                                            }

                                        }
                                    }
                                    /********************** Second constraint*************/
                                    /********************** Third constraint*************/
                                    var indexVarN1 = action.List_Variables.FindIndex(v => v == " n1");
                                    var indexVarIp_Mess = action.List_Variables.FindIndex(v => v == " ip_mess");
                                    if (indexVarN1 != -1 && indexVarIp_Mess != -1)
                                    {
                                        var valueN1 = Int16.Parse(action.List_Values[indexVarN1]);
                                        var valueIp_Mess = Int16.Parse(action.List_Values[indexVarIp_Mess]);

                                        //Expr l = ctx.MkConst("l", ctx.MkIntSort());
                                        Expr zero = ctx.MkNumeral(0, ctx.MkIntSort());
                                        Expr actulValueOfN1 = ctx.MkNumeral(valueN1, ctx.MkIntSort());
                                        Expr actulValueOfIp_Mess = ctx.MkNumeral(valueIp_Mess, ctx.MkIntSort());

                                        BoolExpr Ip_Mess_ValueExpr =
                                            ctx.MkNot(ctx.MkEq(actulValueOfIp_Mess, zero)); // not ip_mess=0
                                        Solver s = ctx.MkSolver();
                                        s.Assert(ctx.MkImplies(ctx.MkEq(actulValueOfN1, zero),
                                            Ip_Mess_ValueExpr)); // N1=0 --> not(ip_mess=0)

                                        Status result = s.Check();
                                        if (result == Status.UNSATISFIABLE)
                                        {
                                            Console.WriteLine(result);
                                            Console.WriteLine("constraint - " + cntrnt +
                                                              "- violated at \n action number: " + action.Id +
                                                              "\n name: " +
                                                              action.Name + "\n value: " + action.Value );
                                        }
                                        else if (result == Status.SATISFIABLE)
                                        {
                                            Console.WriteLine("constraint - " + cntrnt +  " constraint status : " + result + " on " + action.Value);
                                        }
                                        else
                                        {
                                            Console.WriteLine("Uknown sttaus : " + result);
                                        }

                                    }
                                    /********************** Third constraint*************/

                                }


                            }
                        }
                    }
                }
            }
        }

/// <summary>
        /// Check a list of virtual constraints to test execution time
        /// </summary>
        /// <param name="lstConstraints"></param>
        public void checKVirtualConstraint(List<string> lstConstraints)
        {
            int numb_trace = 0;
            using (Context ctx = new Context())
            {
                foreach (var cntrnt in lstConstraints)
                {
                    Solver s = ctx.MkSolver();

                    foreach (var trace in structTracesList)
                    {
                    //numb_trace += 1;
                    //Console.WriteLine("trace number:" + numb_trace);
                    foreach (var action in trace.ActionsList)
                    {
                        // check if this action contains variables in the given constraint
                        if (action.List_Variables.Any(v => cntrnt.Contains(v)))
                        {
                            if (cntrnt.Contains(action.Name))
                            {
                             
                                    /********************** First constraint*************/
                                    //UnsatCoreAndProofExample(ctx);
                                    BoolExpr rec = ctx.MkBoolConst(action.Name); // which is rec

                                    var indexVarL = action.List_Variables.FindIndex(v => v == " l");
                                    if (indexVarL != -1)
                                    {
                                        if (action.List_Values != null)
                                        {
                                            var valueOfL = Int16.Parse(action.List_Values[indexVarL]);

                                            //Expr l = ctx.MkConst("l", ctx.MkIntSort());
                                            Expr one = ctx.MkNumeral(1, ctx.MkIntSort());
                                            Expr actulValueOfL = ctx.MkNumeral(valueOfL, ctx.MkIntSort());


                                            //BoolExpr lValue = ctx.MkEq(l, actulValueOfL);
                                            //Solver s = ctx.MkSolver();
                                            s.Assert(ctx.MkNot(ctx.MkEq(actulValueOfL, one))); // 0<x

                                            Status result = s.Check();
                                            //if (result == Status.UNSATISFIABLE)
                                            //{
                                            //    Console.WriteLine(result);
                                            //    Console.WriteLine("constraint -  " + cntrnt +
                                            //                      "- violated at \n action number: " + action.Id +
                                            //                      "\n name: " +
                                            //                      action.Name + "\n value: " + action.Value);
                                            //}
                                            //else if (result == Status.SATISFIABLE)
                                            //{
                                            //    Console.WriteLine("constraint - " + cntrnt + " constraint status : " + result + " on " + action.Value);
                                            //}
                                            //else
                                            //{
                                            //    Console.WriteLine("Uknown sttaus : " + result);
                                            //}
                                        }

                                    }
                                    /********************** First constraint*************/

                                    /********************** Second constraint*************/

                                    var indexVarB = action.List_Variables.FindIndex(v => v == "b");
                                    if (indexVarB != -1)
                                    {
                                        if (action.List_Values != null)
                                        {
                                            var valueOfB = Int16.Parse(action.List_Values[indexVarB]);

                                            var indexVarBPrevious =
                                                action.Previous_state.variablesList.FindIndex(v => v == " b");
                                            var valueOfBPrevious =
                                                Int16.Parse(action.Previous_state.valuesList[indexVarBPrevious]);
                                            //Expr l = ctx.MkConst("l", ctx.MkIntSort());
                                            Expr bPrevious = ctx.MkNumeral(valueOfBPrevious, ctx.MkIntSort());
                                            Expr actulValueOfB = ctx.MkNumeral(valueOfB, ctx.MkIntSort());


                                            //BoolExpr lValue = ctx.MkEq(l, actulValueOfL);
                                            //Solver s = ctx.MkSolver();
                                            s.Assert(ctx.MkNot(ctx.MkEq(actulValueOfB, bPrevious))); // 0<x

                                            Status result = s.Check();
                                            //if (result == Status.UNSATISFIABLE)
                                            //{
                                            //    Console.WriteLine(result);
                                            //    Console.WriteLine("constraint -  " + cntrnt +
                                            //                      "- violated at \n action number: " + action.Id +
                                            //                      "\n name: " +
                                            //                      action.Name + "\n value: " + action.Value +
                                            //                      "\n Pre: " +
                                            //                      action.Previous_state.value + "\n Post: " +
                                            //                      action.Next_state.value);
                                            //}
                                            //else if (result == Status.SATISFIABLE)
                                            //{
                                            //    Console.WriteLine("constraint - " + cntrnt + " constraint status : " + result + " on " + action.Value);
                                            //}
                                            //else
                                            //{
                                            //    Console.WriteLine("Uknown sttaus : " + result);
                                            //}

                                        }
                                    }
                                    /********************** Second constraint*************/
                                    /********************** Third constraint*************/
                                    //UnsatCoreAndProofExample(ctx);
                                    var indexVarN1 = action.List_Variables.FindIndex(v => v == " n1");
                                    var indexVarIp_Mess = action.List_Variables.FindIndex(v => v == " ip_mess");
                                    if (indexVarN1 != -1 && indexVarIp_Mess != -1)
                                    {
                                        var valueN1 = Int16.Parse(action.List_Values[indexVarN1]);
                                        var valueIp_Mess = Int16.Parse(action.List_Values[indexVarIp_Mess]);

                                        //Expr l = ctx.MkConst("l", ctx.MkIntSort());
                                        Expr zero = ctx.MkNumeral(0, ctx.MkIntSort());
                                        Expr actulValueOfN1 = ctx.MkNumeral(valueN1, ctx.MkIntSort());
                                        Expr actulValueOfIp_Mess = ctx.MkNumeral(valueIp_Mess, ctx.MkIntSort());

                                        BoolExpr Ip_Mess_ValueExpr =
                                            ctx.MkNot(ctx.MkEq(actulValueOfIp_Mess, zero)); // not ip_mess=0
                                        //Solver s = ctx.MkSolver();
                                        s.Assert(ctx.MkImplies(ctx.MkEq(actulValueOfN1, zero),
                                            Ip_Mess_ValueExpr)); // N1=0 --> not(ip_mess=0)

                                        Status result = s.Check();
                                        //if (result == Status.UNSATISFIABLE)
                                        //{
                                        //    Console.WriteLine(result);
                                        //    Console.WriteLine("constraint - " + cntrnt +
                                        //                      "- violated at \n action number: " + action.Id +
                                        //                      "\n name: " +
                                        //                      action.Name + "\n value: " + action.Value);
                                        //}
                                        //else if (result == Status.SATISFIABLE)
                                        //{
                                        //    Console.WriteLine("constraint - " + cntrnt + " constraint status : " + result + " on " + action.Value);
                                        //}
                                        //else
                                        //{
                                        //    Console.WriteLine("Uknown sttaus : " + result);
                                        //}

                                    }
                                    /********************** Third constraint*************/

                                }


                            }
                        }
                    }
                }
            }
        }
    }

}
