using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;

namespace UnityEditor.ShaderGraph
{
    class SliderControl : IShaderControl
    {
        public string[] labels { get; set; }
        public float[] values { get; set; }
        public SerializableValueStore defaultValue { get; }

        public ConcreteSlotValueType[] validPortTypes
        {
            get { return new ConcreteSlotValueType[] { ConcreteSlotValueType.Vector1 }; }
        }

        public SliderControl()
        {
            values = new float[] { 0, 1 };
        }

        public SliderControl(float defaultValue, float minimum, float maximum)
        {
            this.defaultValue = new SerializableValueStore()
            {
                vectorValue = new Vector4(defaultValue, 0.0f, 0.0f, 0.0f)
            };
            values = new float[] { minimum, maximum };
        }

        public VisualElement GetControl(IShaderValue shaderValue)
        {
            VisualElement control = new VisualElement() { name = "SliderControl" };
            control.styleSheets.Add(Resources.Load<StyleSheet>("Styles/ShaderControls/SliderControl"));

            Slider slider = null;
            FloatField floatField = null;

            slider = new Slider(values[0], values[1]) { value = shaderValue.value.vectorValue.x };
            slider.RegisterValueChangedCallback((evt) =>
            {
                if (evt.newValue.Equals(shaderValue.value.vectorValue.x))
                    return;
                floatField.value = evt.newValue;
                shaderValue.UpdateValue(new SerializableValueStore()
                {
                    vectorValue = new Vector4((float)evt.newValue, 0.0f, 0.0f, 0.0f)
                });
            });

            floatField = new FloatField { value = shaderValue.value.vectorValue.x };
            floatField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue.Equals(shaderValue.value.vectorValue.x))
                    return;
                shaderValue.UpdateValue(new SerializableValueStore()
                {
                    vectorValue = new Vector4((float)evt.newValue, 0.0f, 0.0f, 0.0f)
                });
            });
            floatField.Q("unity-text-input").RegisterCallback<FocusOutEvent>(evt =>
            {
                float newValue = Mathf.Max(Mathf.Min(shaderValue.value.vectorValue.x, values[1]), values[0]);
                if (newValue.Equals(shaderValue.value.vectorValue.x))
                    return;
                slider.value = newValue;
                shaderValue.UpdateValue(new SerializableValueStore()
                {
                    vectorValue = new Vector4(newValue, 0.0f, 0.0f, 0.0f)
                });
            });

            control.Add(slider);
            control.Add(floatField);
            return control;
        }
    }
}
