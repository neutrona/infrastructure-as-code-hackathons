using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Diagnostics;

namespace shift.yggdrasil2.Intent
{
    public class IntentTester
    {
        public IntentTesterResults TestResults { get; private set; }

        public Assert Assert { get; private set; }

        public IntentTester()
        {
            this.Assert = new Assert(this);
            this.TestResults = new IntentTesterResults();
        }
    }

    public class Assert
    {
        public IntentTester Tester { get; private set; }

        public void IsNull(string message, object value)
        {
            Test test = new Test(message, MethodBase.GetCurrentMethod().Name);
            test.Success = true;

            if (value != null)
            {
                test.Success = false;
                this.Tester.TestResults.Errors.Add("Assert Error: " + MethodBase.GetCurrentMethod().Name + " - " + message);
            }

            this.Tester.TestResults.Tests.Add(test);
        }

        public void IsNotNull(string message, object value)
        {
            Test test = new Test(message, MethodBase.GetCurrentMethod().Name);
            test.Success = true;

            if (value == null)
            {
                test.Success = false;
                this.Tester.TestResults.Errors.Add("Assert Error: " + MethodBase.GetCurrentMethod().Name + " - " + message);
            }

            this.Tester.TestResults.Tests.Add(test);
        }

        public void IsTrue(string message, bool value)
        {
            Test test = new Test(message, MethodBase.GetCurrentMethod().Name);
            test.Success = true;

            if (!value)
            {
                test.Success = false;
                this.Tester.TestResults.Errors.Add("Assert Error: " + MethodBase.GetCurrentMethod().Name + " - " + message);
            }

            this.Tester.TestResults.Tests.Add(test);
        }

        public void IsFalse(string message, bool value)
        {
            Test test = new Test(message, MethodBase.GetCurrentMethod().Name);
            test.Success = true;

            if (value)
            {
                test.Success = false;
                this.Tester.TestResults.Errors.Add("Assert Error: " + MethodBase.GetCurrentMethod().Name + " - " + message);
            }

            this.Tester.TestResults.Tests.Add(test);
        }

        public void IsEqual(string message, object val1, object val2)
        {
            Test test = new Test(message, MethodBase.GetCurrentMethod().Name);
            test.Success = true;

            if (!val1.Equals(val2))
            {
                test.Success = false;
                this.Tester.TestResults.Errors.Add("Assert Error: " + MethodBase.GetCurrentMethod().Name + " - " + message);
            }

            this.Tester.TestResults.Tests.Add(test);
        }

        public void IsNotEqual(string message, object val1, object val2)
        {
            Test test = new Test(message, MethodBase.GetCurrentMethod().Name);
            test.Success = true;

            if (val1.Equals(val2))
            {
                test.Success = false;
                this.Tester.TestResults.Errors.Add("Assert Error: " + MethodBase.GetCurrentMethod().Name + " - " + message);
            }

            this.Tester.TestResults.Tests.Add(test);
        }

        public void IsGreaterThan<T>(string message, T val1, T val2)
            where T : struct,
          IComparable,
          IComparable<T>,
          IConvertible,
          IEquatable<T>,
          IFormattable
        {
            Test test = new Test(message, MethodBase.GetCurrentMethod().Name);
            test.Success = true;

            if (!(val1.CompareTo(val2) > 0))
            {
                test.Success = false;
                this.Tester.TestResults.Errors.Add("Assert Error: " + MethodBase.GetCurrentMethod().Name + " - " + message);
            }

            this.Tester.TestResults.Tests.Add(test);
        }

        public void IsLessThan<T>(string message, T val1, T val2)
            where T : struct,
          IComparable,
          IComparable<T>,
          IConvertible,
          IEquatable<T>,
          IFormattable
        {
            Test test = new Test(message, MethodBase.GetCurrentMethod().Name);
            test.Success = true;

            if (!(val1.CompareTo(val2) < 0))
            {
                test.Success = false;
                this.Tester.TestResults.Errors.Add("Assert Error: " + MethodBase.GetCurrentMethod().Name + " - " + message);
            }

            this.Tester.TestResults.Tests.Add(test);
        }

        public Assert(IntentTester Tester)
        {
            this.Tester = Tester;
        }
    }

    public class IntentTesterResults
    {
        public List<string> Errors { get; set; }
        public List<Test> Tests { get; set; }

        public IntentTesterResults()
        {
            this.Errors = new List<string>();
            this.Tests = new List<Test>();
        }
    }

    public class Test
    {
        public string Message { get; private set; }
        public string TestType { get; private set; }
        public bool Success { get; set; }

        public Test(string message, string type)
        {
            this.Message = message;
            this.TestType = type;
        }
    }
}
