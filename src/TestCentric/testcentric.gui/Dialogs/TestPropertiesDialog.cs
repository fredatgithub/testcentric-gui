// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric GUI contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace TestCentric.Gui.Dialogs
{
    using Model;
    using NUnit.Engine;
    using Views;

    public partial class TestPropertiesDialog : Form
    {
        private ITestModel _model;
        private ITestTreeView _view;

        private TreeNode _treeNode;
        private TestNode _testNode;
        private ResultNode _resultNode;
        private int maxY;
        private int nextY;
        private int clientWidth;

        private Image pinnedImage;
        private Image unpinnedImage;

        public TestPropertiesDialog(ITestModel model, ITestTreeView view)
        {
            _model = model;
            _view = view;

            InitializeComponent();

            pinnedImage = new Bitmap(GetType().Assembly.GetManifestResourceStream("TestCentric.Gui.Images.pinned.gif"));
            unpinnedImage = new Bitmap(GetType().Assembly.GetManifestResourceStream("TestCentric.Gui.Images.unpinned.gif"));
            pinButton.Image = unpinnedImage;
        }

        #region Properties

        [Browsable(false)]
        public bool Pinned
        {
            get { return pinButton.Checked; }
            set { pinButton.Checked = value; }
        }

        #endregion

        #region Public Methods

        public void DisplayProperties(TreeNode treeNode)
        {
            if (treeNode == null)
                throw new ArgumentNullException(nameof(treeNode));

            _treeNode = treeNode;
            _testNode = (TestNode)treeNode.Tag;
            _resultNode = _model.GetResultForTest(_testNode.Id);

            SetTitleBarText();

            testResult.Text = _resultNode?.Outcome.ToString() ?? _testNode.RunState.ToString();
            testResult.Font = new Font(this.Font, FontStyle.Bold);
            if (_testNode.Type == "Project" || _testNode.Type == "Assembly")
                testName.Text = Path.GetFileName(_testNode.Name);
            else
                testName.Text = _testNode.Name;

            // Display any package info
            var package = _model.GetPackageForTest(_testNode.Id);
            if (package != null)
            {
                FillPackageSettingsList(package);
                groupBox1.Show();
            }
            else
                groupBox1.Hide();

            // Display test details
            groupBox2.Location = package != null
                ? new Point(
                    groupBox1.Location.X, groupBox1.Bottom + 12)
                : groupBox1.Location;

            testType.Text = _testNode.Type;
            fullName.Text = _testNode.FullName;
            description.Text = _testNode.GetProperty("Description");

            categories.Text = _testNode.GetPropertyList("Category");

            testCaseCount.Text = _testNode.TestCount.ToString();

            switch (_testNode.RunState)
            {
                case RunState.Explicit:
                    shouldRun.Text = "Explicit";
                    break;
                case RunState.Runnable:
                    shouldRun.Text = "Yes";
                    break;
                default:
                    shouldRun.Text = "No";
                    break;
            }

            ignoreReason.Text = _testNode.GetProperty("_SKIPREASON");

            FillPropertyList();

            BeginPanel();

            CreateRow(testTypeLabel, testType);
            CreateRow(fullNameLabel, fullName);
            CreateRow(descriptionLabel, description);
            CreateRow(categoriesLabel, categories);
            CreateRow(testCaseCountLabel, testCaseCount, shouldRunLabel, shouldRun);
            CreateRow(ignoreReasonLabel, ignoreReason);
            CreateRow(propertiesLabel, hiddenProperties);
            CreateRow(properties);

            groupBox2.ClientSize = new Size(
                groupBox2.ClientSize.Width, maxY + 12);

            // Display Result GroupBox
            elapsedTime.Text = "Execution Time:";
            assertCount.Text = "Assert Count:";
            message.Text = "";
            stackTrace.Text = "";

            if (_resultNode != null)
            {
                elapsedTime.Text = string.Format("Execution Time: {0}", _resultNode.Duration);

                assertCount.Text = string.Format("Assert Count: {0}", _resultNode.AssertCount);

                // message may have a leading blank line
                // TODO: take care of this in label ?
                var messageText = _resultNode.Message;
                if (messageText != null)
                {
                    if (messageText.Length > 64000)
                        message.Text = TrimLeadingBlankLines(messageText.Substring(0, 64000));
                    else
                        message.Text = TrimLeadingBlankLines(messageText);
                }

                stackTrace.Text = _resultNode.StackTrace;
            }

            groupBox3.Location = new Point(
                groupBox2.Location.X, groupBox2.Bottom + 12);

            BeginPanel();

            CreateRow(elapsedTime, assertCount);
            CreateRow(messageLabel, message);
            CreateRow(stackTraceLabel);
            CreateRow(stackTrace);

            groupBox3.ClientSize = new Size(
                groupBox3.ClientSize.Width, this.maxY + 12);

            this.ClientSize = new Size(
                this.ClientSize.Width, groupBox3.Bottom + 12);

            Show();
        }

        private void FillPropertyList()
        {
            properties.Items.Clear();
            foreach (string entry in _testNode.GetAllProperties(hiddenProperties.Checked))
            {
                properties.Items.Add(entry);
            }
        }

        private void FillPackageSettingsList(TestPackage package)
        {
            packageSettings.Items.Clear();

            foreach (string key in package.Settings.Keys)
            {
                object val = package.Settings[key] ?? "<null>";
                if (val is string && (string)val == string.Empty)
                    val = "<empty>";
                else if (val is string[])
                    val = string.Join(",", val as string[]);
                packageSettings.Items.Add($"{key} = {val}");
            }
        }

        #endregion

        #region Event Handlers and Overrides

        private void pinButton_Click(object sender, System.EventArgs e)
        {
            if (pinButton.Checked)
                pinButton.Image = pinnedImage;
            else
                pinButton.Image = unpinnedImage;
        }

        private void TestPropertiesDialog_ResizeEnd(object sender, EventArgs e)
        {
            if (clientWidth != ClientSize.Width && _treeNode != null)
                DisplayProperties(_treeNode);

            clientWidth = ClientSize.Width;
        }

        protected override bool ProcessKeyPreview(ref System.Windows.Forms.Message m)
        {
            const int ESCAPE = 27;
            const int WM_CHAR = 258;

            if (m.Msg == WM_CHAR && m.WParam.ToInt32() == ESCAPE)
            {
                this.Close();
                return true;
            }

            return base.ProcessKeyEventArgs(ref m);
        }

        private void hiddenProperties_CheckedChanged(object sender, EventArgs e)
        {
            FillPropertyList();
        }

        #endregion

        #region Helper Methods

        private void SetTitleBarText()
        {
            string name = _testNode.Name;
            if (name == null)
                Text = "Properties";
            else
            {
                int index = name.LastIndexOfAny(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
                if (index >= 0)
                    name = name.Substring(index + 1);
                Text = $"{_testNode.Type} Properties - {name}";
            }
        }

        private void BeginPanel()
        {
            this.maxY = 20;
            this.nextY = 24;
        }

        private void SizeToFitText(Label label)
        {
            string text = label.Text;
            if (text == "")
                text = "Ay"; // Include descender to be sure of size

            Graphics g = Graphics.FromHwnd(label.Handle);
            SizeF size = g.MeasureString(text, label.Font, label.Parent.ClientSize.Width - label.Left - 8);
            label.ClientSize = new Size(
                (int)Math.Ceiling(size.Width), (int)Math.Ceiling(size.Height));
        }

        private void CreateRow(params Control[] controls)
        {
            this.nextY = this.maxY + 4;

            foreach (Control control in controls)
            {
                InsertInRow(control);
            }
        }

        private void InsertInRow(Control control)
        {
            Label label = control as Label;
            if (label != null)
                SizeToFitText(label);

            control.Location = new Point(control.Location.X, this.nextY);
            this.maxY = Math.Max(this.maxY, control.Bottom);
        }

        private string TrimLeadingBlankLines(string s)
        {
            if (s == null) return s;

            int start = 0;
            for (int i = 0; i < s.Length; i++)
            {
                switch (s[i])
                {
                    case ' ':
                    case '\t':
                        break;
                    case '\r':
                    case '\n':
                        start = i + 1;
                        break;

                    default:
                        goto getout;
                }
            }

            getout:
            return start == 0 ? s : s.Substring(start);
        }

        #endregion
    }
}
