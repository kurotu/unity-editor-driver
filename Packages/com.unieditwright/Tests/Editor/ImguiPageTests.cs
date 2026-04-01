using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace UniEditWright.Tests
{
    [TestFixture]
    public class PageTests
    {
        private EditorWindow _window;

        [SetUp]
        public void SetUp()
        {
            _window = EditorWindow.GetWindow<TestPlainWindow>();
            _window.Show();
        }

        [TearDown]
        public void TearDown()
        {
            if (_window != null)
                _window.Close();
        }

        // ── Describe / Builder ──────────────────────────────────────

        [Test]
        public void Describe_NullWindowThrows()
        {
            Assert.Throws<ArgumentNullException>(() =>
                Page.Describe(null, p => p.Label("x")));
        }

        [Test]
        public void Describe_NullConfigureThrows()
        {
            Assert.Throws<ArgumentNullException>(() =>
                Page.Describe(_window, null));
        }

        [Test]
        public void Describe_ReturnsPageWithControls()
        {
            var page = Page.Describe(_window, p =>
            {
                p.Label("Title");
                p.TextField("Name");
            });

            Assert.AreEqual(2, page.Controls.Count);
            Assert.AreEqual("Title", page.Controls[0].Label);
            Assert.AreEqual(ControlType.Label, page.Controls[0].Type);
            Assert.AreEqual("Name", page.Controls[1].Label);
            Assert.AreEqual(ControlType.TextField, page.Controls[1].Type);
        }

        [Test]
        public void Describe_AllControlTypes()
        {
            var page = Page.Describe(_window, p =>
            {
                p.Label("L");
                p.TextField("TF");
                p.Toggle("T");
                p.Slider("S", 0, 10);
                p.Button("B");
                p.IntField("I");
                p.FloatField("F");
                p.BeginToggleGroup("TG");
                p.EndToggleGroup();
            });

            Assert.AreEqual(9, page.Controls.Count);
            Assert.AreEqual(ControlType.Label, page.Controls[0].Type);
            Assert.AreEqual(ControlType.TextField, page.Controls[1].Type);
            Assert.AreEqual(ControlType.Toggle, page.Controls[2].Type);
            Assert.AreEqual(ControlType.Slider, page.Controls[3].Type);
            Assert.AreEqual(ControlType.Button, page.Controls[4].Type);
            Assert.AreEqual(ControlType.IntField, page.Controls[5].Type);
            Assert.AreEqual(ControlType.FloatField, page.Controls[6].Type);
            Assert.AreEqual(ControlType.ToggleGroup, page.Controls[7].Type);
            Assert.AreEqual(ControlType.EndToggleGroup, page.Controls[8].Type);
        }

        [Test]
        public void Describe_SliderStoresMinMax()
        {
            var page = Page.Describe(_window, p =>
            {
                p.Slider("Amount", -5f, 5f);
            });

            Assert.AreEqual(-5f, page.Controls[0].SliderMin, 0.001f);
            Assert.AreEqual(5f, page.Controls[0].SliderMax, 0.001f);
        }

        [Test]
        public void Describe_LabelWithCustomStyle()
        {
            var page = Page.Describe(_window, p =>
            {
                p.Label("Bold", EditorStyles.boldLabel);
            });

            Assert.AreSame(EditorStyles.boldLabel, page.Controls[0].CustomStyle);
        }

        // ── Layout Calculation ──────────────────────────────────────

        [Test]
        public void Resolve_AssignsPositiveRects()
        {
            var page = Page.Describe(_window, p =>
            {
                p.Label("Title");
                p.TextField("Name");
                p.Toggle("Enabled");
            });

            foreach (var control in page.Controls)
            {
                Assert.Greater(control.Rect.width, 0, $"Control '{control.Label}' width should be positive");
                Assert.Greater(control.Rect.height, 0, $"Control '{control.Label}' height should be positive");
            }
        }

        [Test]
        public void Resolve_ControlsStackVertically()
        {
            var page = Page.Describe(_window, p =>
            {
                p.Label("A");
                p.TextField("B");
                p.Toggle("C");
            });

            for (int i = 1; i < page.Controls.Count; i++)
            {
                Assert.Greater(page.Controls[i].Rect.y, page.Controls[i - 1].Rect.y,
                    $"Control '{page.Controls[i].Label}' should be below '{page.Controls[i - 1].Label}'");
            }
        }

        [Test]
        public void Resolve_EndToggleGroupHasZeroRect()
        {
            var page = Page.Describe(_window, p =>
            {
                p.BeginToggleGroup("Group");
                p.Toggle("Inner");
                p.EndToggleGroup();
            });

            var endGroup = page.Controls[2];
            Assert.AreEqual(ControlType.EndToggleGroup, endGroup.Type);
            Assert.AreEqual(Rect.zero, endGroup.Rect);
        }

        [Test]
        public void Resolve_ControlsHaveExpectedHeight()
        {
            var page = Page.Describe(_window, p =>
            {
                p.TextField("Name");
            });

            Assert.AreEqual(EditorGUIUtility.singleLineHeight, page.Controls[0].Rect.height, 0.01f);
        }

        [Test]
        public void Refresh_RecomputesRects()
        {
            var page = Page.Describe(_window, p =>
            {
                p.TextField("Name");
            });

            var rectBefore = page.Controls[0].Rect;

            // Simulate window resize
            var pos = _window.position;
            _window.position = new Rect(pos.x, pos.y, pos.width + 100, pos.height);

            page.Refresh();

            Assert.Greater(page.Controls[0].Rect.width, rectBefore.width,
                "Rect width should increase after window widened");
        }

        // ── GetByLabel ──────────────────────────────────────────────

        [Test]
        public void GetByLabel_ReturnsLocator()
        {
            var page = Page.Describe(_window, p =>
            {
                p.TextField("Email");
            });

            var locator = page.GetByLabel("Email");

            Assert.IsNotNull(locator);
            Assert.AreEqual("Email", locator.Label);
            Assert.AreEqual(ControlType.TextField, locator.ControlType);
        }

        [Test]
        public void GetByLabel_ThrowsForUnknownLabel()
        {
            var page = Page.Describe(_window, p =>
            {
                p.TextField("Email");
            });

            Assert.Throws<InvalidOperationException>(() => page.GetByLabel("NonExistent"));
        }

        [Test]
        public void GetByLabel_NullLabelThrows()
        {
            var page = Page.Describe(_window, p =>
            {
                p.TextField("Email");
            });

            Assert.Throws<ArgumentNullException>(() => page.GetByLabel(null));
        }

        // ── Locator interaction ────────────────────────────────

        [UnityTest]
        public IEnumerator Locator_Click_DoesNotThrow()
        {
            yield return null;

            var page = Page.Describe(_window, p =>
            {
                p.Label("Title");
                p.TextField("Name");
            });

            Assert.DoesNotThrow(() => page.GetByLabel("Name").Click());
        }

        [UnityTest]
        public IEnumerator Locator_Fill_DoesNotThrow()
        {
            yield return null;

            var page = Page.Describe(_window, p =>
            {
                p.Label("Title");
                p.TextField("Name");
            });

            Assert.DoesNotThrow(() => page.GetByLabel("Name").Fill("hello"));
        }

        [UnityTest]
        public IEnumerator Locator_Toggle_DoesNotThrow()
        {
            yield return null;

            var page = Page.Describe(_window, p =>
            {
                p.Toggle("Enabled");
            });

            Assert.DoesNotThrow(() => page.GetByLabel("Enabled").Toggle());
        }

        [UnityTest]
        public IEnumerator Locator_SetSlider_DoesNotThrow()
        {
            yield return null;

            var page = Page.Describe(_window, p =>
            {
                p.Slider("Amount", 0, 1);
            });

            Assert.DoesNotThrow(() => page.GetByLabel("Amount").SetSlider(0.5f));
        }

        // ── Test helper ─────────────────────────────────────────────

        /// <summary>
        /// Plain IMGUI window with no UniEditWright dependency — the whole point.
        /// </summary>
        public class TestPlainWindow : EditorWindow
        {
            public string StringValue = "test";
            public bool BoolValue = true;
            public float FloatValue = 0.5f;
            public bool GroupEnabled = true;

            void OnGUI()
            {
                GUILayout.Label("Test Label", EditorStyles.boldLabel);
                StringValue = EditorGUILayout.TextField("Text", StringValue);
                BoolValue = EditorGUILayout.Toggle("Check", BoolValue);
                FloatValue = EditorGUILayout.Slider("Amount", FloatValue, 0f, 1f);
                GroupEnabled = EditorGUILayout.BeginToggleGroup("Group", GroupEnabled);
                EditorGUILayout.EndToggleGroup();
            }
        }
    }
}
