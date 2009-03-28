using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Extensibility;
using Microsoft.VisualStudio.CommandBars;

using NEnhancer.Common;

namespace NEnhancer
{
    /// <summary>The object for implementing an Add-in.</summary>
    /// <seealso class='IDTExtensibility2' />
    public class Connect : IDTExtensibility2, IDTCommandTarget
    {
        #region Command Names

        private static readonly string COMMAND_VIEWER_COMMAND_NAME = "CommandViewer";
        private static readonly string CLOSE_ALL_DOCUMENTS_COMMAND_NAME = "CloseAllDocuments";
        private static readonly string CODE_CONVERTER_COMMAND_NAME = "CodeConverter";
        private static readonly string NPETSHOP_SLN_GENERATOR_COMMAND_NAME = "NPetshopSlnGenerator";
        private static readonly string COLLAPSE_ALL_PROJECTS_COMMAND_NAME = "CollapseAllProjects";
        private static readonly string INSERT_COMMENT_COMMAND_NAME = "InsertComment";

        #endregion

        #region CommandBarEvents

        CommandBarEvents convertToAutoPropCmdEvent;
        CommandBarEvents convertToNormalPropCmdEvent;
        CommandBarEvents batchPropertyCmdEvent;

        #endregion

        private DTE2 _applicationObject;
        private AddIn _addInInstance;
        private DTEHelper helper;

        public Connect()
        {
        }

        #region IDTExtensibility2 interface methods

        public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
        {
            _applicationObject = (DTE2)application;
            _addInInstance = (AddIn)addInInst;
            helper = new DTEHelper(_applicationObject, _addInInstance);

            if (connectMode == ext_ConnectMode.ext_cm_UISetup)
            {
                // Get main menu bar
                CommandBar menuBarCommandBar =
                    ((CommandBars)_applicationObject.CommandBars)["MenuBar"];

                #region Add CommandViewer command

                // TODO: So many hard coded strings...
                // Get Tools menu
                string toolsMenuName = helper.GetCulturedMenuName("Tools");
                CommandBarControl toolsControl = menuBarCommandBar.Controls[toolsMenuName];
                CommandBarPopup toolsPopup = (CommandBarPopup)toolsControl;

                // Add a new command
                helper.AddNamedCommand2(toolsPopup.CommandBar, COMMAND_VIEWER_COMMAND_NAME, "CommandViewer",
                    "View All Commands", true, 59, toolsPopup.Controls.Count + 1);

                #endregion

                #region Add CloseAllDocuments command

                // Get "Easy MDI Document Window" command bar
                CommandBar mdiDocCommandBar = helper.GetCommandBarByName("Easy MDI Document Window");
                // Place the command below "Close All But This" menu item
                CommandBarControl closeAllButThisCmd = mdiDocCommandBar.Controls["Close All But This"];
                int closeAllCmdIndex = (closeAllButThisCmd == null) ? 1 : (closeAllButThisCmd.Index + 1);
                // Add a new command
                helper.AddNamedCommand2(mdiDocCommandBar, CLOSE_ALL_DOCUMENTS_COMMAND_NAME,
                    "Close All Documents", "Close All Documents", false, 0, closeAllCmdIndex);

                #endregion

                #region Add NPetshopSlnGenerator command

                helper.AddNamedCommand2(toolsPopup.CommandBar, NPETSHOP_SLN_GENERATOR_COMMAND_NAME, "Generate NPetshop Sln",
                    "Generate NPetshop Solution", true, 59, toolsPopup.Controls.Count + 1);

                #endregion

                #region Add CollapseAllProjects command

                // Get "Solution Explorer" command bar
                CommandBar slnCommandBar = helper.GetCommandBarByName("Solution");
                // Add a new command
                helper.AddNamedCommand2(slnCommandBar, COLLAPSE_ALL_PROJECTS_COMMAND_NAME,
                    "Collapse All Projects", "Collapse All Projects", false, 0, slnCommandBar.Controls.Count + 1);

                #endregion

                #region Add InsertComment command

                // Get "Code Window" command bar
                CommandBar codeWinCommandBar = helper.GetCommandBarByName("Code Window");
                //// Add a new command
                //AddNamedCommand2(codeWinCommandBar, INSERT_COMMENT_COMMAND_NAME,
                //    "-", "Insert Comment", false, 0, codeWinCommandBar.Controls.Count + 1);

                #endregion

                #region Add PropertyManager commands

                int pmPopupIndex = codeWinCommandBar.Controls.Count + 1;
                CommandBarPopup pmPopup = codeWinCommandBar.Controls.Add(
                    MsoControlType.msoControlPopup, Type.Missing, Type.Missing,
                    pmPopupIndex, true) as CommandBarPopup;
                pmPopup.Caption = "PropertyManager";

                CommandBarButton batchPropertyCmd = helper.AddButtonToPopup(pmPopup, pmPopup.Controls.Count + 1,
                    "Batch Property", "Encapsulate these fields");
                batchPropertyCmdEvent = _applicationObject.Events.get_CommandBarEvents(batchPropertyCmd) as CommandBarEvents;
                batchPropertyCmdEvent.Click += new _dispCommandBarControlEvents_ClickEventHandler(BatchPropertyCmdEvent_Click);

                CommandBarButton convertToAutoPropCmd = helper.AddButtonToPopup(pmPopup, pmPopup.Controls.Count + 1,
                    "Convert to Auto-Property", "Convert to Auto-Property(.NET 3.0 style)");
                convertToAutoPropCmdEvent = _applicationObject.Events.get_CommandBarEvents(convertToAutoPropCmd) as CommandBarEvents;
                convertToAutoPropCmdEvent.Click += new _dispCommandBarControlEvents_ClickEventHandler(ConvertToAutoPropCmdEvent_Click);

                //CommandBarButton convertToNormalPropCmd = AddButtonToPopup(pmPopup, pmPopup.Controls.Count + 1,
                //    "Convert to Normal-Property", "Convert to Normal-Property(.NET 2.0 style)");
                //convertToNormalPropCmdEvent = _applicationObject.Events.get_CommandBarEvents(convertToNormalPropCmd) as CommandBarEvents;
                //convertToNormalPropCmdEvent.Click += new _dispCommandBarControlEvents_ClickEventHandler(ConvertToNormalPropCmdEvent_Click);

                #endregion
            }
        }

        public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom)
        {
        }

        public void OnAddInsUpdate(ref Array custom)
        {
        }

        public void OnStartupComplete(ref Array custom)
        {
        }

        public void OnBeginShutdown(ref Array custom)
        {
        }

        #endregion

        #region IDTCommandTarget interface methods

        public void QueryStatus(string commandName, vsCommandStatusTextWanted neededText,
            ref vsCommandStatus status, ref object commandText)
        {
            if (neededText == vsCommandStatusTextWanted.vsCommandStatusTextWantedNone)
            {
                if (commandName == GetCommandFullName(COMMAND_VIEWER_COMMAND_NAME))
                {
                    status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
                    return;
                }
                else if (commandName == GetCommandFullName(CLOSE_ALL_DOCUMENTS_COMMAND_NAME))
                {
                    status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
                    return;
                }
                else if (commandName == GetCommandFullName(NPETSHOP_SLN_GENERATOR_COMMAND_NAME))
                {
                    status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
                    return;
                }
                else if (commandName == GetCommandFullName(COLLAPSE_ALL_PROJECTS_COMMAND_NAME))
                {
                    status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
                    return;
                }
                else if (commandName == GetCommandFullName(INSERT_COMMENT_COMMAND_NAME))
                {
                    status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
                    return;
                }
            }
        }

        public void Exec(string commandName, vsCommandExecOption executeOption,
            ref object varIn, ref object varOut, ref bool handled)
        {
            handled = false;
            if (executeOption == vsCommandExecOption.vsCommandExecOptionDoDefault)
            {
                if (commandName == GetCommandFullName(COMMAND_VIEWER_COMMAND_NAME))
                {
                    ShowCmdBarViewer();

                    handled = true;
                    return;
                }
                else if (commandName == GetCommandFullName(CLOSE_ALL_DOCUMENTS_COMMAND_NAME))
                {
                    CloseAllDocuments();

                    handled = true;
                    return;
                }
                else if (commandName == GetCommandFullName(NPETSHOP_SLN_GENERATOR_COMMAND_NAME))
                {
                    GenerateNPetshopSln();

                    handled = true;
                    return;
                }
                else if (commandName == GetCommandFullName(COLLAPSE_ALL_PROJECTS_COMMAND_NAME))
                {
                    CollapseAllProjects();

                    handled = true;
                    return;
                }
                else if (commandName == GetCommandFullName(INSERT_COMMENT_COMMAND_NAME))
                {
                    InsertComments();

                    handled = true;
                    return;
                }
            }
        }

        #endregion

        private string GetCommandFullName(string cmdName)
        {
            return "NEnhancer.Connect." + cmdName;
        }

        #region Command Handlers

        private void ShowCmdBarViewer()
        {
            CommandBarViewer viewerForm = new CommandBarViewer();
            viewerForm.DTEObject = _applicationObject;
            viewerForm.ShowDialog();
        }

        private void CloseAllDocuments()
        {
            _applicationObject.ExecuteCommand("Window.CloseAllDocuments", string.Empty);
        }

        private void GenerateNPetshopSln()
        {
            NPetshopSlnGenerator generator = new NPetshopSlnGenerator();
            generator.DTEObject = _applicationObject;
            generator.ShowDialog();
        }

        private void ConvertSolution()
        {
            CodeConverter converter = new CodeConverter();
            converter.DTEObject = _applicationObject;
            converter.ShowDialog();
        }

        #region CollapseAllProjects

        private void CollapseAllProjects()
        {
            Solution sln = _applicationObject.Solution;
            List<UIHierarchyItem> projects = helper.GetProjectNodes(sln);
            foreach (UIHierarchyItem item in projects)
            {
                CollapseProject(item);
            }
        }

        private void CollapseProject(UIHierarchyItem project)
        {
            if (project.UIHierarchyItems.Expanded)
            {
                if (helper.IsDirectProjectNode(project))
                {
                    project.UIHierarchyItems.Expanded = false;
                }
                else if (helper.IsProjectNodeInSolutionFolder(project))
                {
                    project.Select(vsUISelectionType.vsUISelectionTypeSelect);
                    helper.SolutionExplorerNode.DoDefaultAction();
                }
            }
        }

        #endregion

        private void InsertComments()
        {
            if (_applicationObject.ActiveWindow.Object is TextWindow)
            {
                MessageBox.Show("TextWindow");
            }
            else if (_applicationObject.ActiveWindow.Object is HtmlWindow)
            {
                MessageBox.Show("HtmlWindow");
            }
        }

        #region Property Manager

        // Rules
        // field: camelCase, property: PascalCase;
        // check all the selected lines instead of the selected text.
        private void BatchPropertyCmdEvent_Click(object CommandBarControl, ref bool Handled, ref bool CancelDefault)
        {
            string selectedLines = helper.GetSelectedLines();
            GenerateProperties(selectedLines);
        }

        private void GenerateProperties(string selectedLines)
        {
            Regex regex = new Regex(".*(?<type>\\b\\w+\\b)\\s+(?<fieldName>\\w+)(\\s*=.+)*;", RegexOptions.IgnoreCase);
            MatchCollection fieldLines = regex.Matches(selectedLines);

            StringBuilder props = new StringBuilder();
            foreach (Match fieldLine in fieldLines)
            {
                string type = fieldLine.Groups["type"].Value;
                string fieldName = fieldLine.Groups["fieldName"].Value;
                bool readOnly = fieldLine.Value.IndexOf("readonly") >= 0;
                props.AppendLine(GenerateNormalProperty(type, fieldName, readOnly));
            }

            Clipboard.SetText(props.ToString());
        }

        private string GenerateNormalProperty(string type, string fieldName, bool readOnly)
        {
            string propertyName = fieldName.Substring(0, 1).ToUpper() + fieldName.Substring(1);
            // TODO: Add a config item to indicate which modifier will be used by default.
            // Add a NewLine to use VS' auto-format feature.
            string format = "public {0} {1}{{ " + Environment.NewLine + "{2} {3} }}";
            string getter = string.Format("get{{ return {0}; }}", fieldName);
            string setter = string.Empty;
            if (!readOnly)
            {
                setter = string.Format("set {{ {0} = value; }}", fieldName);
            }

            return string.Format(format, type, propertyName, getter, setter);
        }

        private void ConvertToAutoPropCmdEvent_Click(object CommandBarControl, ref bool Handled, ref bool CancelDefault)
        {
            string selectedLines = helper.GetSelectedLines();
            Regex propRegex = new Regex(
                "((?<modifier>\\b\\w+\\b)\\s+)*(?<type>\\b\\w+\\b)\\s+(?<name>\\w+)\\s*{(.|\\n)+?(?<!;\\s*)}",
                RegexOptions.IgnoreCase | RegexOptions.Multiline);
            MatchCollection properties = propRegex.Matches(selectedLines);

            StringBuilder autoProps = new StringBuilder();
            foreach (Match prop in properties)
            {
                string modifier = prop.Groups["modifier"].Value;
                if (modifier.Length == 0)
                {
                    modifier = "private";
                }
                string type = prop.Groups["type"].Value;
                string name = prop.Groups["name"].Value;

                string propText = prop.Value;
                bool hasGetter = Regex.IsMatch(propText, "get\\s*{(.|\\n)+?}");
                bool hasSetter = Regex.IsMatch(propText, "set\\s*{(.|\\n)+?}");
                if (!hasGetter && !hasSetter) { break; }

                // Generate a prop
                autoProps.AppendLine(GenerateAutoProperty(modifier, type, name, hasGetter, hasSetter));
            }

            Clipboard.SetText(autoProps.ToString());
        }

        private string GenerateAutoProperty(string modifier, string type, string name, bool hasGetter, bool hasSetter)
        {
            string format = "{0} {1} {2}{{ {3} {4} }}";
            string getter = "get;";
            if (!hasGetter)
            {
                getter = "private " + getter;
            }

            string setter = "set;";
            if (!hasSetter)
            {
                setter = "private " + setter;
            }

            return string.Format(format, modifier, type, name, getter, setter);
        }

        //private void ConvertToNormalPropCmdEvent_Click(object CommandBarControl, ref bool Handled, ref bool CancelDefault)
        //{
            
        //}

        #endregion

        #endregion

        // auto prop
        // "\\w+\\s+(?<name>\\w+)\\s*{\\s*get;(.|\\n)*set;\\s*}"
    }
}