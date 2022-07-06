// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric GUI contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System.Collections.Generic;
using System.Windows.Forms;

namespace TestCentric.Gui.Presenters
{
    using Model;
    using Views;

    /// <summary>
    /// TestGrouping is the base class of all groupings. It represents a
    /// set of TestGroups containing tests. In the base class, the contents
    /// of each group is stable once created.
    /// </summary>
    public abstract class TestGrouping
    {
        protected GroupDisplayStrategy _displayStrategy;

        #region Constructor

        /// <summary>
        /// Construct a TestGrouping for a given GroupDisplayStrategy
        /// </summary>
        /// <param name="displayStrategy"></param>
        public TestGrouping(GroupDisplayStrategy displayStrategy)
        {
            _displayStrategy = displayStrategy;
            Groups = new List<TestGroup>();
        }

        #endregion

        #region Public Members

        public abstract string ID { get; }

        public List<TestGroup> Groups { get; private set; }

        /// <summary>
        /// Load tests into the grouping
        /// </summary>
        /// <param name="tests">Tests to be loaded</param>
        public virtual void Load(IEnumerable<TestNode> tests)
        {
            foreach (TestNode testNode in tests)
                foreach (var group in SelectGroups(testNode))
                    group.Add(testNode);
        }

        /// <summary>
        /// Post a test result to the tree, changing the treeNode
        /// color to reflect success or failure.
        /// </summary>
        public virtual void OnTestFinished(ResultNode result)
        {
            // Override to take any necessary action
        }

        /// <summary>
        /// Returns an array of groups in which a TestNode is categorized.
        /// </summary>
        public abstract TestGroup[] SelectGroups(TestNode testNode);

        #endregion
    }
}
