using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Printing;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.Remoting.Channels;

namespace Bonsai.SpikeGLX
{
    /// <summary>
    /// Represents an operator that sets one or more NI digital output lines through SpikeGLX from
    /// a sequence of values representing the state of the line.
    /// </summary>
    [Combinator]
    [WorkflowElementCategory(ElementCategory.Sink)]
    [TypeConverter(typeof(WaveArmConverter))]
    [Description("TO-DO.")]
    public class WaveArm
    {
        /// <summary>
        /// Gets or sets the IP address of the SpikeGLX command server.
        /// </summary>
        [Category("Command Server")]
        [Description("The IP address of the SpikeGLX command server.")]
        public string Host { get; set; } = "localhost";

        /// <summary>
        /// Gets or sets the port of the SpikeGLX command server.
        /// </summary>
        [Category("Command Server")]
        [Description("The port of the SpikeGLX command server.")]
        public int Port { get; set; } = 4142;

        [RefreshProperties(RefreshProperties.All)]
        [Category("Device")]
        public DeviceType DeviceType { get; set; } = DeviceType.Daq;

        [Category("Device")]
        public int Ip { get; set; }

        [Category("Device")]
        public int Slot { get; set; }

        public int Trig { get; set; }

        [Category("Device")]
        public string OutChan { get; set; }

        public string TrigTerm { get; set; }

        public int Loop { get; set; }

        /// <summary>
        /// Sets one or more NI digital output lines through SpikeGLX from an observable sequence
        /// of unsigned integer values representing a digital state.
        /// </summary>
        /// <param name="source">
        /// A sequence of 32-bit unsigned integers, where each value represents a bitmask of 
        /// states to which to set the digital output lines.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the <paramref name="source"/> sequence
        /// but where there is an additional side effect of setting digital output lines through
        /// SpikeGLX.
        /// </returns>
        /// <remarks>
        /// The lowest 8 bits of the bitmask map to port0, the next higher 8 bits to port1, etc.
        /// This mapping is fixed, irrespective of whether only a subset of lines have been
        /// selected in <see cref="Channels"/>. The effect of selecting a subset of lines
        /// in <see cref="Channels"/> is to ignore those bits in the bitmask corresponding
        /// to unlisted lines. 
        /// </remarks>
        public IObservable<T> Process<T>(IObservable<T> source)
        {
            return Observable.Using(() => new SpikeGLX(Host, Port),
                connection =>
                {
                    switch (DeviceType)
                    {
                        case DeviceType.Daq:
                            return source.Do(input => connection.NIWaveArm(OutChan, TrigTerm));
                        case DeviceType.Onebox:
                            return source.Do(input => connection.ObxWaveArm(Ip, Slot, Trig, Loop));
                        default:
                            return source;
                    }
                });
        }

    }

    public class WaveArmConverter : ExpandableObjectConverter
    {

        public override bool GetPropertiesSupported(ITypeDescriptorContext context) => true;

        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            // Get all default properties
            var properties = TypeDescriptor.GetProperties(value, attributes, true);
            var op = value as WaveArm;

            if (op == null) return properties;

            List<string> blacklist = new List<string> { };
            if (op.DeviceType == DeviceType.Daq)
            {
                blacklist.Add("Ip");
                blacklist.Add("Slot");
                blacklist.Add("Trig");
                blacklist.Add("Loop");
            }

            if (op.DeviceType == DeviceType.Onebox)
            {
                blacklist.Add("OutChan");
                blacklist.Add("TrigTerm");
            }

            var filtered = properties.Cast<PropertyDescriptor>()
                .Where(p => !(blacklist.Contains(p.Name)))
                .ToArray();

            return new PropertyDescriptorCollection(filtered);
        }

    }

}
