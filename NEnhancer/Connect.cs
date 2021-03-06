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
using NEnhancer.Logic.CodeTemplate;
using NEnhancer.Logic.SearchText;

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
        private static readonly string SHOW_SHORTCUT_LIST_COMMAND_NAME = "ShowShortcutList";

        #endregion

        #region CommandBarEvents

        CommandBarEvents batchPropertyCmdEvent;
        CommandBarEvents convertToAutoPropCmdEvent;
        CommandBarEvents convertToNormalPropCmdEvent;
        CommandBarEvents[] codeTemplateCmdEvents;
        CommandBarEvents codeModelCmdEvent;
        CommandBarEvents[] searchTextCmdEvents;

        #endregion

        #region Custom tool windows

        private Window shortcutListWindow;
        private ShortcutListControl shortcutListCtrl;

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

                #region Add ShortcutList command

                // Get View menu
                string viewMenuName = helper.GetCulturedMenuName("View");
                CommandBarControl viewControl = menuBarCommandBar.Controls[viewMenuName];
                CommandBarPopup viewPopup = (CommandBarPopup)viewControl;
                helper.AddNamedCommand2(viewPopup.CommandBar, SHOW_SHORTCUT_LIST_COMMAND_NAME, "Show shortcut list window",
                    "Show shortcut list window", false, 0, viewPopup.Controls.Count + 1);

                #endregion

                #region Add CollapseAllProjects command

                // Get "Solution Explorer" command bar
                CommandBar slnCommandBar = helper.GetCommandBarByName("Solution");
                // Add a new command
                helper.AddNamedCommand2(slnCommandBar, COLLAPSE_ALL_PROJECTS_COMMAND_NAME,
                    "Collapse All Projects", "Collapse All Projects", false, 0, slnCommandBar.Controls.Count + 1);

                #endregion

                #region Add PropertyManager commands

                // Get "Code Window" command bar
                CommandBar codeWinCommandBar = helper.GetCommandBarByName("Code Window");

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
                    "Convert to Auto-Property", "Convert to Auto-Property(C# 3.0 style)");
                convertToAutoPropCmdEvent = _applicationObject.Events.get_CommandBarEvents(convertToAutoPropCmd) as CommandBarEvents;
                convertToAutoPropCmdEvent.Click += new _dispCommandBarControlEvents_ClickEventHandler(ConvertToAutoPropCmdEvent_Click);

                CommandBarButton convertToNormalPropCmd = helper.AddButtonToPopup(pmPopup, pmPopup.Controls.Count + 1,
                    "Convert to Normal-Property", "Convert to Normal-Property(C# 2.0 style)");
                convertToNormalPropCmdEvent = _applicationObject.Events.get_CommandBarEvents(convertToNormalPropCmd) as CommandBarEvents;
                convertToNormalPropCmdEvent.Click += new _dispCommandBarControlEvents_ClickEventHandler(ConvertToNormalPropCmdEvent_Click);

                #endregion

                #region Add CodeTemplate commands

                // Add popup menu item
                int templatePopupIndex = codeWinCommandBar.Controls.Count + 1;
                CommandBarPopup codeTemplatePopup = codeWinCommandBar.Controls.Add(
                    MsoControlType.msoControlPopup, Type.Missing, Type.Missing,
                    templatePopupIndex, true) as CommandBarPopup;
                codeTemplatePopup.Caption = "Code Template";

                List<string> templateNames = CodeTemplateManager.Instance.GetTemplateNames();
                codeTemplateCmdEvents = new CommandBarEvents[templateNames.Count];
                for (int i = 0; i < templateNames.Count; i++)
                {
                    string name = templateNames[i];
                    CommandBarButton codeTemplateCmd = helper.AddButtonToPopup(codeTemplatePopup, codeTemplatePopup.Controls.Count + 1,
                        name, "Insert this code template");
                    codeTemplateCmdEvents[i] = _applicationObject.Events.get_CommandBarEvents(codeTemplateCmd) as CommandBarEvents;
                    codeTemplateCmdEvents[i].Click += new _dispCommandBarControlEvents_ClickEventHandler(codeTemplateCmdEvent_Click);
                }

                #endregion

                #region Add Code Model commands

                //CommandBarButton codeModelCmd = helper.AddButtonToCmdBar(codeWinCommandBar, codeWinCommandBar.Controls.Count + 1,
                //    "Code Model", "Code Model Sample");
                //codeModelCmdEvent = _applicationObject.Events.get_CommandBarEvents(codeModelCmd) as CommandBarEvents;
                //codeModelCmdEvent.Click += new _dispCommandBarControlEvents_ClickEventHandler(CodeModelCmdEvent_Click);

                #endregion

                #region Search This commands

                int searchThisPopupIndex = codeWinCommandBar.Controls.Count + 1;
                CommandBarPopup searchThisPopup = codeWinCommandBar.Controls.Add(
                    MsoControlType.msoControlPopup, Type.Missing, Type.Missing,
                    searchThisPopupIndex, true) as CommandBarPopup;
                searchThisPopup.Caption = "Search This In";

                List<SearchEngine> engines = SearchEngineConfigManager.Instance.GetSearchEngines();
                searchTextCmdEvents = new CommandBarEvents[engines.Count];
                for (int i = 0; i < engines.Count; i++)
                {
                    SearchEngine e = engines[i];
                    CommandBarButton searchCmd = helper.AddButtonToPopup(searchThisPopup, searchThisPopup.Controls.Count + 1,
                        e.Name, "Search in " + e.Name);
                    if (i == 0)
                    {
                        searchCmd.FaceId = 141;
                    }
                    searchTextCmdEvents[i] = _applicationObject.Events.get_CommandBarEvents(searchCmd) as CommandBarEvents;
                    searchTextCmdEvents[i].Click += new _dispCommandBarControlEvents_ClickEventHandler(SearchTextCmdEvent_Click);
                }

                #endregion
            }
            else if (connectMode == ext_ConnectMode.ext_cm_AfterStartup)
            {
                object programmingObj = null;
                string guidString = "{41F8DEA8-EB07-45b7-9B1D-EB969DC43EC5}";
                Windows2 windows2 = _applicationObject.Windows as Windows2;
                Assembly asm = Assembly.GetExecutingAssembly();
                shortcutListWindow = windows2.CreateToolWindow2(_addInInstance, asm.Location,
                    "NEnhancer.ShortcutListControl", "Visual Studio Shortcut List",
                    guidString, ref programmingObj);
                shortcutListCtrl = shortcutListWindow.Object as ShortcutListControl;
                shortcutListCtrl.DTEObject = _applicationObject;
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
                else if (commandName == GetCommandFullName(SHOW_SHORTCUT_LIST_COMMAND_NAME))
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
                else if (commandName == GetCommandFullName(SHOW_SHORTCUT_LIST_COMMAND_NAME))
                {
                    shortcutListWindow.Visible = true;

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

        private void LaunchUrlInDefaultBrowser(string url)
        {
            System.Diagnostics.Process.Start(url);
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

        #region Property Manager

        private void BatchPropertyCmdEvent_Click(object CommandBarControl, ref bool Handled, ref bool CancelDefault)
        {
            string selectedLines = helper.GetSelectedLines();
            GenerateProperties(selectedLines);
        }

        private void GenerateProperties(string selectedLines)
        {
            Regex regex = new Regex(".*(?<type>\\b\\w+\\b)\\s+(?<fieldName>\\w+)(\\s*=.+)*;", RegexOptions.IgnoreCase);
            MatchCollection fieldLines = regex.Matches(selectedLines);

            // 约定字段为camelCase, 属性为PascalCase
            StringBuilder props = new StringBuilder();
            foreach (Match fieldLine in fieldLines)
            {
                string type = fieldLine.Groups["type"].Value;
                string fieldName = fieldLine.Groups["fieldName"].Value;
                bool readOnly = fieldLine.Value.IndexOf("readonly") >= 0;
                props.AppendLine(GenerateNormalProperty("public", type, fieldName, readOnly, false));
            }

            Clipboard.SetText(props.ToString());
        }

        private string GenerateNormalProperty(string modifier, string type, string fieldName, bool readOnly, bool writeOnly)
        {
            string propertyName = StringHelper.GetPascalCaseString(fieldName);
            // 添加一个换行可利用VS的自动格式特性
            string format = "{0} {1} {2}{{ " + Environment.NewLine + "{3} {4} }}";

            string getter = string.Empty;
            if (!writeOnly)
            {
                getter = string.Format("get{{ return {0}; }}", fieldName);
            }

            string setter = string.Empty;
            if (!readOnly)
            {
                setter = string.Format("set {{ {0} = value; }}", fieldName);
            }

            return string.Format(format, modifier, type, propertyName, getter, setter);
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

        private void ConvertToNormalPropCmdEvent_Click(object CommandBarControl, ref bool Handled, ref bool CancelDefault)
        {
            string selectedLines = helper.GetSelectedLines();
            Regex propRegex = new Regex(
                "((?<modifier>\\w+)?\\s+)?(?<type>\\w+)\\s+(?<name>\\w+)\\s*{(.|\\n)*?get;(.|\\n)*?set;\\s*}",
                RegexOptions.IgnoreCase | RegexOptions.Multiline);
            MatchCollection autoProperties = propRegex.Matches(selectedLines);

            StringBuilder normalProps = new StringBuilder();
            foreach (Match prop in autoProperties)
            {
                string modifier = prop.Groups["modifier"].Value;
                string type = prop.Groups["type"].Value;
                string name = prop.Groups["name"].Value;

                string propText = prop.Value;
                bool writeOnly = Regex.IsMatch(propText, "private\\s+get;");
                bool readOnly = Regex.IsMatch(propText, "private\\s+set;");
                if (writeOnly && readOnly) { break; }

                normalProps.AppendLine(GenerateNormalProperty(modifier, type, name, readOnly, writeOnly));
            }

            Clipboard.SetText(normalProps.ToString());
        }

        #endregion

        #region CodeTemplate Manager

        private void codeTemplateCmdEvent_Click(object CommandBarControl, ref bool Handled, ref bool CancelDefault)
        {
            CommandBarControl ctrl = CommandBarControl as CommandBarControl;
            string content = CodeTemplateManager.Instance.GetTemplateContent(ctrl.Caption);

            int indexOfSelectedParam = CodeTemplateManager.Instance.IndexOfSelectedParam(content);
            bool surroundSelectedText = (indexOfSelectedParam >= 0);

            TextSelection selected = _applicationObject.ActiveDocument.Selection as TextSelection;
            EditPoint topPoint = selected.TopPoint.CreateEditPoint();
            EditPoint bottomPoint = selected.BottomPoint.CreateEditPoint();

            if (surroundSelectedText)
            {
                string beforeSelectedParam =
                    CodeTemplateManager.Instance.GetTextBeforeSelectedParam(content);
                string afterSelectedParam =
                    CodeTemplateManager.Instance.GetTextAfterSelectedParam(content);

                topPoint.LineUp(1);
                topPoint.EndOfLine();
                topPoint.Insert(Environment.NewLine);
                topPoint.Insert(beforeSelectedParam);

                bottomPoint.EndOfLine();
                bottomPoint.Insert(Environment.NewLine);
                bottomPoint.Insert(afterSelectedParam);
            }
            else
            {
                topPoint.Delete(bottomPoint);
                topPoint.Insert(content);
            }
        }

        private int GetIndentionSize(string line)
        {
            int size = 0;
            for (int i = 0; i < line.Length; i++)
            {
                if (!Char.IsWhiteSpace(line[i]))
                {
                    size = i;
                    break;
                }
            }

            return size;
        }

        #endregion

        #region Code Model

        private void CodeModelCmdEvent_Click(object CommandBarControl, ref bool Handled, ref bool CancelDefault)
        {
            TextSelection selected = _applicationObject.ActiveDocument.Selection as TextSelection;
            //EditPoint topPoint = selected.TopPoint.CreateEditPoint();
            //EditPoint bottomPoint = selected.BottomPoint.CreateEditPoint();
            TextPoint pnt = (TextPoint)selected.ActivePoint;
            vsCMElement scopes = 0;
            string elems = string.Empty;

            foreach (vsCMElement scope in Enum.GetValues(scopes.GetType()))
            {
                CodeElement elem = pnt.get_CodeElement(scope);
                if (elem != null)
                {
                    elems += elem.Name + " (" + scope.ToString() + ") \n";
                }
            }

            MessageBox.Show(elems);
        }

        #endregion

        #region Search This

        private void SearchTextCmdEvent_Click(object CommandBarControl, ref bool Handled, ref bool CancelDefault)
        {
            string engineName = (CommandBarControl as CommandBarControl).Caption;
            string urlFormat = SearchEngineConfigManager.Instance.GetSearchEngines().Find(e => e.Name.Equals(engineName)).Url;
            LaunchUrlInDefaultBrowser(string.Format(urlFormat, helper.GetSelectedText()));
        }

        #endregion

        #endregion
    }
}