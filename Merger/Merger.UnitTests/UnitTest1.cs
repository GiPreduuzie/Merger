using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Merger.UnitTests
{
    public class Utility
    {
        internal string ApplyCommand(string code, Command command)
        {
            if (command is RenameCommand)
            {
                var renameCommand = command as RenameCommand;

                return code.Replace(renameCommand.Variable, renameCommand.NewName);
            }

            throw new Exception();
        }
    }

  

    public class UserQuestion
    {
        public UserAnswer BeingAsked(string question)
        {
            return new UserAnswer(question);
        }
    }

    public class UserAnswer
    {
        public UserAnswer(string question)
        {
            Question = question;
        }

        public string Question { get; private set; }
        public UserDialog Answer(string answer) { return new UserDialog(Question, answer); }
    }

   

    public class Commands
    {
        public Command Rename(string classFullName, string method, string variable, string newName)
        {
            return new RenameCommand(classFullName, method, variable, newName);
        }
    }

    public class CodeManager
    {
        public CodeManager(string code)
        {
            Code = code;
        }

        public string Code { get; private set; }

        internal BranchesManager ForkBranches()
        {
            return new BranchesManager(Code);
        }
    }

   

    public class BranchesManager
    {
        private string _code;
        public List<Command> _leftCommands = new List<Command>();
        public List<Command> _rightCommands = new List<Command>();

        public BranchesManager(string code)
        {
            _code = code;
        }

        public BranchesManager AddLeft(Command command)
        {
            _leftCommands.Add(command);
            return this;
        }

        public BranchesManager AddRight(Command command)
        {
            _rightCommands.Add(command);
            return this;
        }

        public CodeManager MergeBranches(UserDialog dialog)
        {
            var leftCommand = _leftCommands.Single();
            var rightCommand = _rightCommands.Single();

            var commands = new ConflictResolver().ResolveConflict(dialog, leftCommand, rightCommand);

            var utility = new Utility();

            return new CodeManager(commands.Aggregate(_code, (accum, command) => utility.ApplyCommand(accum, command)));
        }
    }

    public class Library
    {
        internal CodeManager GetCode(string fileName)
        {
            return new CodeManager(File.ReadAllText(@"C:\Dev\Merger\Library\" + fileName + ".cs"));
        }
    }

    public class TestBase
    {
        public Library Library => new Library();
        public Commands Commands { get { return new Commands(); } }
        public UserQuestion User { get { return new UserQuestion(); } }
        public MessagesGenerator MessagesGenerator { get { return new MessagesGenerator(); } }
        public Utility Utility { get { return new Utility(); } }
    }

    [TestClass]
    public class GeneralCases : TestBase
    {
        [TestMethod]
        public void RenameVariable_Confilct()
        {
            var startCode = Library.GetCode("Ex_1");

            var resultCode =

            startCode
                .ForkBranches()
                .AddLeft(Commands.Rename("GeneralCase", "Method", "variable", "variable1"))
                .AddRight(Commands.Rename("GeneralCase", "Method", "variable", "variable2"))
                .MergeBranches(User.BeingAsked(MessagesGenerator.NameConflict("variable1", "variable2")).Answer("variable3"))
                .Code;

            Assert.AreEqual(Utility.ApplyCommand(startCode.Code, Commands.Rename("GeneralCase", "Method", "variable", "variable3")), resultCode);
        }

        [TestMethod]
        public void Rename2Variables_NoConfilct()
        {
            var startCode = Library.GetCode("Ex_1");

            var resultCode =

            startCode
                .ForkBranches()
                .AddLeft(Commands.Rename("GeneralCase", "Method", "variable", "variable1"))
                .AddRight(Commands.Rename("GeneralCase", "Method", "tail", "tail1"))
                .MergeBranches(User.BeingAsked(MessagesGenerator.NameConflict("variable1", "variable2")).Answer("variable3"))
                .Code;

            Assert.AreEqual(Utility.ApplyCommand(startCode.Code, Commands.Rename("GeneralCase", "Method", "variable", "variable3")), resultCode);
        }
    }
}
