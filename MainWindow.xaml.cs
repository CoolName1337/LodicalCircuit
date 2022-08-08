using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;


namespace LogicalCircuit
{
    public enum NodeValue
    {
        Low,
        High
    }

    public abstract class Render
    {
        static public SolidColorBrush Red = new SolidColorBrush(Colors.Red);
        static public SolidColorBrush Gray = new SolidColorBrush(Colors.Gray);
        static public SolidColorBrush GhostWhite = new SolidColorBrush(Colors.GhostWhite);
        static public SolidColorBrush Blue = new SolidColorBrush(Color.FromRgb(63, 100, 75));
        static public SolidColorBrush Green = new SolidColorBrush(Color.FromRgb(63, 75, 100));

        protected Shape sprite;
        public Shape Sprite { get => sprite; set => sprite = value; }

        private Point position;
        public virtual Point Position
        {
            get => position;
            set
            {
                position = value;
                Canvas.SetTop(Sprite, position.Y);
                Canvas.SetLeft(Sprite, position.X);
            }
        }
        private static Panel _field;
        static public Panel Field
        {
            get => _field;
            set
            {
                _field = value;
                Canvas.SetTop(_field, 0);
                Canvas.SetLeft(_field, 0);
            }
        }
        public Render(Point pos, Shape sprite)
        {
            Sprite = sprite;
            Position = pos;
            Sprite.DataContext = this;
        }
        public virtual void Remove()
        {
            Field.Children.Remove(this.Sprite);
        }

        public virtual int ZIndex
        {
            get => Canvas.GetZIndex(Sprite);
            set => Canvas.SetZIndex(Sprite, value);
        }

        public abstract Render Copy();
        public virtual void AddToField()
        {
            Field.Children.Add(Sprite);
        }
    }
    public class ChangeInputEventArgs : EventArgs
    {
        public readonly NodeValue NewValue;
        public ChangeInputEventArgs(NodeValue val) => NewValue = val;
    }

    public enum NodeType
    {
        Static,
        StaticInverse,
        Dynamic,
        DynamicInverse
    }

    public static class Symbols
    {
        public static Shape GetShapeSymbol(NodeType nt)
        {
            switch (nt)
            {
                case NodeType.StaticInverse:
                    return new Ellipse() { Fill = Render.GhostWhite, Stroke = Render.Gray, StrokeThickness = 2, Width = 15, Height = 15 };
                case NodeType.Dynamic:
                    return new Line() { Stroke = Render.Gray, StrokeThickness = 5, X1 = -6, Y1 = 12, X2 = 6, Y2 = -12, Height = 18 };
                case NodeType.DynamicInverse:
                    return new Line() { Stroke = Render.Gray, StrokeThickness = 5, X1 = -6, Y1 = -12, X2 = 6, Y2 = 12, Height = 18 };
            }
            return new Line();
        }
    }

    public abstract class Node : Render
    {
        public LogicalElement parentLE;
        protected int m_recursionCount;
        public event EventHandler<ChangeInputEventArgs> ChangeValue;
        protected NodeValue value;
        private NodeType type;
        public void SetName(string name)
        {
            textName.Text = name ?? "";
        }
        public string GetName() => textName.Text;
        public NodeType GetNodeType() => type;
        public void SetSymbol(NodeType nodeType)
        {
            type = nodeType;
            Field.Children.Remove(symbol);
            symbol = Symbols.GetShapeSymbol(nodeType);
            Position = Position;
        }
        private Shape symbol = new Line();
        private TextBlock textName = new TextBlock() { FontSize = 22 };
        protected readonly Line line = new Line() { StrokeThickness = 10, Stroke = Gray };
        public override Point Position
        {
            get => base.Position;
            set
            {
                base.Position = value;
                line.X1 = Position.X + Sprite.Width / 2;
                line.X2 = parentLE?.Position.X ?? line.X1;
                line.Y2 = line.Y1 = Position.Y + Sprite.Height / 2;
                Canvas.SetTop(textName, Position.Y - Sprite.Height);
                Canvas.SetLeft(textName, Position.X + (line.X2 - Position.X > 0 ? 30 : -30));
                if (symbol != null)
                {
                    Canvas.SetZIndex(symbol, 3);
                    if (symbol is Line)
                    {
                        Canvas.SetTop(symbol, Position.Y + symbol.Height / 2);
                        Canvas.SetLeft(symbol, Position.X + (line.X2 - Position.X > 0 ? 50 : -40));
                    }
                    else
                    {
                        Canvas.SetTop(symbol, Position.Y + symbol.Height / 5);
                        Canvas.SetLeft(symbol, Position.X + (line.X2 - Position.X > 0 ? 45 : -50));
                    }
                }
            }
        }
        public override void Remove()
        {
            base.Remove();
            Field.Children.Remove(line);
            Field.Children.Remove(textName);
            Field.Children.Remove(symbol);
        }
        public override void AddToField()
        {
            base.AddToField();
            Canvas.SetZIndex(line, -3);
            Canvas.SetZIndex(textName, 3);
            Canvas.SetZIndex(symbol, 3);
            Field.Children.Add(line);
            Field.Children.Add(textName);
            Field.Children.Add(symbol);
        }

        public virtual NodeValue Value
        {
            get => value;
            set => this.value = value;
        }
        public Node(Point pos = default) : base(pos, new Ellipse() { Width = 20, Height = 20, StrokeThickness = 5, Fill = Gray }) { }
        protected Line wire;
        public abstract void Connect(Node node);
        public abstract void Disconnect(Node node);

        public void OnChangeValue()
        {
            ChangeValue?.Invoke(this, new ChangeInputEventArgs(Value));
        }
    }

    public class Input : Node
    {
        public Output ConnectedOutput;
        public Line Wire
        {
            get => wire;
            set
            {
                wire = value;
                if (value != null) Position = Position;
            }
        }
        public override Point Position
        {
            get => base.Position;
            set
            {
                base.Position = value;
                if (wire != null)
                {
                    wire.X2 = Position.X + Sprite.Width / 2;
                    wire.Y2 = Position.Y + Sprite.Height / 2;
                }
            }

        }
        public void SetValue()
        {

            Value = ConnectedOutput?.Value ?? NodeValue.Low;
            Sprite.Fill = Value == NodeValue.High ? Red : Gray;
            if (ConnectedOutput != null)
                ConnectedOutput.AllChangeValues += () => OnChangeValue();
        }
        public override void Connect(Node node)
        {
            Output output = node as Output;
            Wire = new Line() { StrokeThickness = 10, Stroke = Gray };
            Wire.X1 = node.Position.X + Sprite.Height / 2;
            Wire.Y1 = node.Position.Y + Sprite.Height / 2;
            Canvas.SetZIndex(Wire, -3);
            if (Field.Children.Contains(this.Sprite))
            {
                Field.Children.Add(wire);
            }
            ConnectedOutput = output;
            output.Inputs.Add(this);
            SetValue();
            OnChangeValue();

        }
        public override void Disconnect(Node node)
        {
            if (node is Output output)
            {
                Field.Children.Remove(wire);
                Wire = null;
                ConnectedOutput = null;
                output.Inputs.Remove(this);
                SetValue();
                OnChangeValue();
            }
        }
        public override void Remove()
        {
            Disconnect(ConnectedOutput);
            base.Remove();
        }
        public override Render Copy()
        {
            Input temp = new Input();
            temp.SetSymbol(GetNodeType());
            temp.SetName(GetName());
            return temp;
        }
    }
    public class Output : Node
    {
        public List<Input> Inputs = new List<Input>();
        public Action AllChangeValues;
        public override Point Position
        {
            get => base.Position;
            set
            {
                base.Position = value;
                if (Inputs?.Count > 0)
                {
                    foreach (Input input in Inputs)
                    {
                        if (input.Wire != null)
                        {
                            input.Wire.X1 = Position.X + Sprite.Width / 2;
                            input.Wire.Y1 = Position.Y + Sprite.Height / 2;
                        }
                    }
                }
            }
        }
        public override NodeValue Value
        {
            get => value;
            set => SetValue(value);
        }
        public void SetValue(NodeValue value)
        {
            if (value != this.value)
            {
                if (m_recursionCount > 10)
                {
                    MessageBox.Show("Exception(((... delete maybe bad wire");
                    m_recursionCount = 0; 
                    try {
                        Inputs.ForEach(input => input.Disconnect(this));
                    }
                    catch (InvalidOperationException) { }
                    return;
                }
                m_recursionCount++;
                this.value = value;
                Sprite.Fill = value == NodeValue.High ? Red : Gray;
                //Parallel.ForEach(Inputs, input => input.SetValue());
                Inputs.ForEach(input => input.SetValue());
                AllChangeValues?.Invoke();
                AllChangeValues = null;
            }
            m_recursionCount = 0;
        }
        public override void Connect(Node node)
        {
            Input input = node as Input;
            input.Disconnect(input.ConnectedOutput);
            if (input.ConnectedOutput != this) input.Connect(this);

        }
        public override void Disconnect(Node node)
        {
            Input input = node as Input;
            if (input?.ConnectedOutput == this) input.Disconnect(this);

        }

        public override void Remove()
        {
            try
            {
                foreach (Input input in Inputs)
                    input.Disconnect(this);
            }
            catch (InvalidOperationException)
            {
                this.Remove();
            }
            finally
            {
                base.Remove();
            }
        }
        public override Render Copy()
        {
            Output temp = new Output();
            temp.SetSymbol(GetNodeType());
            temp.SetName(GetName());
            return temp;

        }
    }
    public class LogicalElement : Render
    {
        public Input[] Inputs;
        public Output[] Outputs;
        private Action<object, ChangeInputEventArgs, Input[], Output[]> Logic;
        public readonly Label label = new Label();
        protected string name;
        private bool isFlipX = false;
        public bool IsFlipX
        {
            get => isFlipX;
            set
            {
                isFlipX = value;
                Position = Position;
            }
        }
        public void FlipX()
        {
            IsFlipX = !IsFlipX;
        }
        public override Point Position
        {
            get => base.Position;
            set
            {
                base.Position = value;
                for (int i = 0; i < (Inputs?.Length ?? 0); i++)
                {
                    if (Inputs[i] == null) continue;
                    Inputs[i].Position = new Point(this.Position.X + (isFlipX ? Sprite.Width + 40 : -50), this.Position.Y + Sprite.Height * (i + 1) / (Inputs.Length + 1));
                }
                for (int i = 0; i < (Outputs?.Length ?? 0); i++)
                {
                    if (Outputs[i] == null) continue;
                    Outputs[i].Position = new Point(this.Position.X + (isFlipX ? -50 : Sprite.Width + 40), this.Position.Y + Sprite.Height * (i + 1) / (Outputs.Length + 1));
                }
                label.RenderTransform = new ScaleTransform(isFlipX ? -1 : 1, 1);
                Canvas.SetTop(label, Position.Y);
                Canvas.SetLeft(label, Position.X + (isFlipX ? Sprite.Width : 0));
            }
        }

        public override int ZIndex
        {
            get => base.ZIndex;
            set
            {
                base.ZIndex = value;
                Canvas.SetZIndex(label, value);
                foreach (Node inp in Inputs)
                    inp.ZIndex = value;
                foreach (Node outp in Outputs)
                    outp.ZIndex = value;
            }
        }
        protected static int _largerInt(int x, int y) => x > y ? x : y;
        public LogicalElement(int InputsCount, int OutputsCount, Action<object, ChangeInputEventArgs, Input[], Output[]> logic, Point position, string text, string symbol) :
            base(position, new Rectangle()
            {
                Width = 100,
                Height = InputsCount > 3 || OutputsCount > 3 ? _largerInt(OutputsCount, InputsCount) * 50 : 150,
                Fill = GhostWhite,
                Stroke = Gray,
                StrokeThickness = 5,
                StrokeLineJoin = PenLineJoin.Round,
            })
        {
            Logic = logic;
            label.Content = symbol;
            name = text;
            label.FontSize = 44;
            label.DataContext = this;
            Inputs = new Input[InputsCount];
            for (int i = 0; i < Inputs.Length; i++)
            {
                Inputs[i] = new Input();
                Inputs[i].parentLE = this;
                Inputs[i].ChangeValue += UpdateOutputValues;
            }
            Outputs = new Output[OutputsCount];
            for (int i = 0; i < Outputs.Length; i++)
            {
                Outputs[i] = new Output();
                Outputs[i].parentLE = this;
            }
            Position = position;
            UpdateOutputValues(null, null);
        }
        protected void UpdateOutputValues(object sender, ChangeInputEventArgs e)
        {
            Logic?.Invoke(sender, e, Inputs, Outputs);
        }
        static public NodeValue NOT(NodeValue val) => val == NodeValue.High ? NodeValue.Low : NodeValue.High;
        public override void Remove()
        {
            foreach (Input input in Inputs)
                input.Remove();
            foreach (Output output in Outputs)
                output.Remove();
            Field.Children.Remove(label);
            base.Remove();
        }
        public override Render Copy() => new LogicalElement(Inputs.Length, Outputs.Length, Logic, Position, name, label.Content?.ToString());
        public override void AddToField()
        {
            base.AddToField();
            Field.Children.Add(label);
            foreach (Output outp in Outputs) outp?.AddToField();
            foreach (Input inp in Inputs) inp?.AddToField();
        }
        public override string ToString() => name ?? label.Content?.ToString();
    }

    public class ButtonNode : Render
    {
        public bool isClicked;
        public Output[] outputs;
        public Rectangle Clicker = new Rectangle() { Width = 60, Height = 60, Fill = GhostWhite, Stroke = Gray, StrokeThickness = 5 };
        public ButtonNode(Point pos) :
            base(pos, new Rectangle() { Width = 85, Height = 85, Fill = GhostWhite, Stroke = Gray, StrokeThickness = 5 })
        {
            outputs = new Output[4];
            for (int i = 0; i < outputs.Length; i++)
            {
                outputs[i] = new Output();
            }
            Position = pos;
            Clicker.DataContext = this;
        }
        public override Point Position
        {
            get => base.Position;
            set
            {
                base.Position = value;
                if (outputs != null)
                {
                    outputs[0].Position = Position - new Vector(outputs[0].Sprite.Width / 2, outputs[0].Sprite.Height / 2);
                    outputs[1].Position = Position + new Vector(-outputs[1].Sprite.Width / 2 + Sprite.Width, -outputs[1].Sprite.Height / 2 + Sprite.Height);
                    outputs[2].Position = Position + new Vector(-outputs[2].Sprite.Width / 2 + Sprite.Width, -outputs[2].Sprite.Height / 2);
                    outputs[3].Position = Position + new Vector(-outputs[3].Sprite.Width / 2, -outputs[3].Sprite.Height / 2 + Sprite.Height);
                }
                Canvas.SetTop(Clicker, Position.Y - Clicker.Height / 2 + Sprite.Height / 2);
                Canvas.SetLeft(Clicker, Position.X - Clicker.Width / 2 + Sprite.Width / 2);
            }
        }
        public override int ZIndex
        {
            get => base.ZIndex;
            set
            {
                base.ZIndex = value;
                Canvas.SetZIndex(Clicker, value);
                foreach (Node outp in outputs)
                    outp.ZIndex = value;
            }
        }
        public void Click()
        {
            Clicker.Fill = Clicker.Fill == GhostWhite ? Red : GhostWhite;
            foreach (Output output in outputs)
            {
                output.Value = LogicalElement.NOT(output.Value);
            }
        }
        public override void Remove()
        {
            foreach (Output output in outputs)
                output.Remove();
            Field.Children.Remove(Clicker);
            base.Remove();
        }
        public override Render Copy() => new ButtonNode(Position);
        public override void AddToField()
        {
            base.AddToField();
            Field.Children.Add(Clicker);
            foreach (Output outp in outputs) outp.AddToField();
        }
        public override string ToString() => $"Button";
    }

    class Light : Render
    {
        public Input[] inputs;
        public Rectangle Lamp = new Rectangle() { Width = 90, Height = 90, Fill = GhostWhite, Stroke = Gray, StrokeThickness = 3 };
        public Light(Point pos) :
            base(pos, new Rectangle() { Width = 100, Height = 100, Fill = GhostWhite, Stroke = Gray, StrokeThickness = 3 })
        {
            inputs = new Input[4];
            for (int i = 0; i < inputs.Length; i++)
            {
                inputs[i] = new Input();
                inputs[i].ChangeValue += Light_ChangeValue;
            }
            Position = pos;
            Lamp.DataContext = this;
        }

        private void Light_ChangeValue(object sender, ChangeInputEventArgs e) => Lamp.Fill = inputs.Any(input => input.Value == NodeValue.High) ? Red : GhostWhite;

        public override Point Position
        {
            get => base.Position;
            set
            {
                base.Position = value;
                if (inputs != null)
                {
                    inputs[0].Position = Position - new Vector(inputs[0].Sprite.Width / 2, inputs[0].Sprite.Height / 2);
                    inputs[1].Position = Position + new Vector(-inputs[1].Sprite.Width / 2 + Sprite.Width, -inputs[1].Sprite.Height / 2 + Sprite.Height);
                    inputs[2].Position = Position + new Vector(-inputs[2].Sprite.Width / 2 + Sprite.Width, -inputs[2].Sprite.Height / 2);
                    inputs[3].Position = Position + new Vector(-inputs[3].Sprite.Width / 2, -inputs[3].Sprite.Height / 2 + Sprite.Height);
                }
                Canvas.SetTop(Lamp, Position.Y - Lamp.Height / 2 + Sprite.Height / 2);
                Canvas.SetLeft(Lamp, Position.X - Lamp.Width / 2 + Sprite.Width / 2);
            }
        }
        public override int ZIndex
        {
            get => base.ZIndex;
            set
            {
                base.ZIndex = value;
                Canvas.SetZIndex(Lamp, value);
                foreach (Node inp in inputs)
                    inp.ZIndex = value;
            }
        }
        public override void Remove()
        {
            foreach (Input input in inputs)
                input.Remove();
            Field.Children.Remove(Lamp);
            base.Remove();
        }
        public override Render Copy() => new Light(Position);
        public override void AddToField()
        {
            base.AddToField();
            Field.Children.Add(Lamp);
            foreach (Input inp in inputs) inp.AddToField();
        }
        public override string ToString() => $"Light";
    }

    public class ButtonSpawn
    {
        public static TextBlock delButtonTextBlock;
        public static Button cancelButton;
        public static ButtonSpawn buttonToDelete;
        public static Panel deletePanel;

        public static bool isWantToDeleteButton;
        static private List<ButtonSpawn> AllButtonToSpawn = new List<ButtonSpawn>();
        public Grid Handle = new Grid();
        public Shape Sprite = new Rectangle()
        {
            Fill = new SolidColorBrush(Colors.Azure),
            Height = Stack.Width,
            Stroke = Render.Gray,
            StrokeThickness = 6,
            StrokeLineJoin = PenLineJoin.Round,
            RadiusX = 40,
            RadiusY = 30
        };
        static public Panel Stack;
        public Render rend;
        public ButtonSpawn(Render rend)
        {
            Handle.MouseEnter += Handle_MouseEnter;
            Handle.MouseLeave += Handle_MouseLeave;
            Handle.MouseLeftButtonUp += MouseClick;
            Handle.DataContext = this;
            Handle.Children.Add(Sprite);
            Handle.Children.Add(new TextBlock()
            {
                Text = rend.ToString(),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                FontSize = rend.ToString().Count() > 15 ? Stack.Width / rend.ToString().Count() * 3 : 40
            });
            this.rend = rend.Copy();
            rend.Remove();
            Add(this);
        }
        public Render Spawn()
        {
            Render temp = rend.Copy();
            temp.AddToField();
            return temp;
        }
        private void Handle_MouseEnter(object sender, MouseEventArgs e)
        {

            if (((FrameworkElement)sender).DataContext is ButtonSpawn but)
            {
                but.Sprite.Opacity = 0.5;
                if (isWantToDeleteButton && Array.IndexOf(AllButtonToSpawn.ToArray(), but) > 5)
                {
                    but.Sprite.Fill = Render.Red;
                }
            }
        }
        private void MouseClick(object sender, MouseEventArgs e)
        {
            if (isWantToDeleteButton && ((FrameworkElement)sender).DataContext is ButtonSpawn but && Array.IndexOf(AllButtonToSpawn.ToArray(), but) > 5)
            {
                deletePanel.Visibility = Visibility.Visible;
                delButtonTextBlock.Text = rend.ToString();
                buttonToDelete = but;
            }
        }
        private void Handle_MouseLeave(object sender, MouseEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is ButtonSpawn but)
            {
                but.Sprite.Opacity = 1;
                but.Sprite.Fill = Render.GhostWhite;
            }
        }
        static public void Add(ButtonSpawn buttonSpawn)
        {
            AllButtonToSpawn.Add(buttonSpawn);
            Stack.Children.Add(buttonSpawn.Handle);
        }
        static public void Remove(ButtonSpawn buttonSpawn)
        {
            AllButtonToSpawn.Remove(buttonSpawn);
            Stack.Children.Remove(buttonSpawn.Handle);
        }

    }

    class LogicalGroup : LogicalElement
    {
        private LogicalElement[] m_logicalElements;
        public LogicalGroup(LogicalElement[] logicalElements, Input[] inputs, Output[] outputs, Point position, string name, string symbol) : base(0,0,null, position, name, symbol)
        {
            Sprite = new Rectangle()
            {
                Width = 100,
                Height = inputs.Length > 3 || outputs.Length > 3 ? _largerInt(outputs.Length, inputs.Length) * 50 : 150,
                Fill = GhostWhite,
                Stroke = Gray,
                StrokeThickness = 5,
                StrokeLineJoin = PenLineJoin.Round,
            };
            Sprite.DataContext = this;
            m_logicalElements = new LogicalElement[logicalElements.Length];
            for (int i = 0; i < logicalElements.Length; i++)
                m_logicalElements[i] = logicalElements[i].Copy() as LogicalElement;
            for (int elIndex = 0; elIndex < m_logicalElements.Length; elIndex++)
            {
                for (int outIndex = 0; outIndex < m_logicalElements[elIndex].Outputs.Length; outIndex++)
                {
                    for (int inpIndex = 0; inpIndex < logicalElements[elIndex].Outputs[outIndex].Inputs.Count; inpIndex++)
                    {
                        Input input = logicalElements[elIndex].Outputs[outIndex].Inputs[inpIndex];
                        int elemToConnectInd = Array.IndexOf(logicalElements, input.parentLE);
                        if (elemToConnectInd == -1) continue;
                        int inputToConnectIn = Array.IndexOf(logicalElements[elemToConnectInd].Inputs.ToArray(), input);
                        m_logicalElements[elIndex].Outputs[outIndex]
                            .Connect(m_logicalElements[elemToConnectInd]
                            .Inputs[inputToConnectIn]);
                    }
                }
            }
            Inputs = new Input[inputs.Length];
            for (int i = 0; i < inputs.Length; i++)
            {
                for (int j = 0; j < logicalElements.Length; j++)
                {
                    int inpResInd = Array.IndexOf(logicalElements[j].Inputs, inputs[i]);
                    if (inpResInd != -1)
                    {
                        Inputs[i] = m_logicalElements[j].Inputs[inpResInd];
                        Inputs[i].SetName(inputs[i].GetName());
                        Inputs[i].SetSymbol(inputs[i].GetNodeType());
                        Inputs[i].parentLE = this;
                    }
                }
            }
            Outputs = new Output[outputs.Length];
            for (int i = 0; i < outputs.Length; i++)
            {
                for (int j = 0; j < logicalElements.Length; j++)
                {
                    int outResInd = Array.IndexOf(logicalElements[j].Outputs, outputs[i]);
                    if (outResInd != -1)
                    {
                        Outputs[i] = m_logicalElements[j].Outputs[outResInd];
                        Outputs[i].SetName(outputs[i].GetName());
                        Outputs[i].SetSymbol(outputs[i].GetNodeType());
                        Outputs[i].parentLE = this;
                    }
                }
            }
            Position = position;
        }
        public override Render Copy() => new LogicalGroup(m_logicalElements, Inputs, Outputs, Position, name, label.Content?.ToString());

    }
    public partial class MainWindow : Window
    {

        private Render[] RenderBuffer;

        private bool m_isWantToCreateNewElement;
        private List<Node> m_takedNodesToNewElements = new List<Node>();

        private bool isWantToMoveField;
        private Point m_oldWheelPosition;

        public static bool isLeftButtonDown;
        private bool isWantToClick;

        private Line wire;
        public Node takedNode;
        private List<Render> takedElements = new List<Render>();

        private Node takedNodeForRename;

        private double k_Size = 1;
        public void InitializeField()
        {
            Field.RenderTransform = new ScaleTransform(k_Size, k_Size);
            Render.Field = handledField;
            ButtonSpawn.Stack = buttonsStack;
            ButtonSpawn.deletePanel = DeletePanel;
            ButtonSpawn.delButtonTextBlock = textBlockNameOfDletedButton;

            new ButtonSpawn(new LogicalElement(1, 1, 
                (sender, e, inputs, outputs) => outputs[0].Value = inputs[0].Value, 
                new Point(0, 0), "▷", "▷"));
            new ButtonSpawn(new LogicalElement(1, 1, 
                (sender, e, inputs, outputs) => outputs[0].Value = LogicalElement.NOT(inputs[0].Value), 
                new Point(0, 0), "Not", "!"));
            new ButtonSpawn(new LogicalElement(2, 1, 
                (sender, e, inputs, outputs) => outputs[0].Value = inputs[0].Value & inputs[1].Value, 
                new Point(0, 0), "And", "&"));
            new ButtonSpawn(new LogicalElement(2, 1, 
                (sender, e, inputs, outputs) => outputs[0].Value = inputs[0].Value | inputs[1].Value, 
                new Point(0, 0), "Or", "|"));

            new ButtonSpawn(new ButtonNode(new Point()));
            new ButtonSpawn(new Light(new Point()));
        }

        public MainWindow()
        {
            InitializeComponent();
            InitializeField();
        }
        public bool TrySpriteToObject<T>(object element, out T obj) where T : class
        {
            obj = (element as FrameworkElement)?.DataContext as T;
            return obj != null;
        }

        private void AddToNodes(Node node)
        {
            node.Sprite.StrokeThickness = 5;
            node.Sprite.Stroke = node is Input ? Render.Blue : Render.Green;
            m_takedNodesToNewElements.Add(node);
        }
        private void RemoveFromNodes(Node node)
        {
            if (m_isWantToCreateNewElement)
            {
                node.Sprite.Stroke = node.Sprite.Fill;
                node.Sprite.StrokeThickness = 0;
                m_takedNodesToNewElements.Remove(node);
            }
        }
        private void ClearAllNodes()
        {
            foreach (Node node in m_takedNodesToNewElements)
            {
                node.Sprite.Stroke = node.Sprite.Fill;
                node.Sprite.StrokeThickness = 0;
            }
            m_takedNodesToNewElements.Clear();
        }
        private void AddToTakedElements(Render elem)
        {
            elem.ZIndex = 2;
            elem.Sprite.Stroke = Render.Red;
            takedElements.Add(elem);
        }
        private void RemoveFromTakedElements(Render elem)
        {
            elem.ZIndex = -2;
            elem.Sprite.Stroke = Render.Gray;
            takedElements.Remove(elem);
        }
        private void ClearTakedElements()
        {
            foreach (Render rend in takedElements)
            {
                rend.ZIndex = -2;
                rend.Sprite.Stroke = Render.Gray;
            }
            takedElements.Clear();
        }

        private void Field_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!(e.Source is TextBox) && !(e.Source is ComboBox) && !(e.Source is TextBlock) && !(e.Source is Button))
                CanvasSetNameAndSymbolForNode.Visibility = Visibility.Hidden;
            if (!(e.Source is Button))
                MoreAction.Visibility = Visibility.Hidden;
            if (ButtonSpawn.isWantToDeleteButton) return;
            isLeftButtonDown = e.LeftButton == MouseButtonState.Pressed;
            if (TrySpriteToObject(e.Source, out ButtonSpawn buttonSpawn))
            {
                ClearTakedElements();
                AddToTakedElements(buttonSpawn.Spawn());
                Field_MouseMove(sender, e);
            }
            else if (TrySpriteToObject(e.Source, out Node node))
            {
                if (!Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    takedNode = node;
                    wire = new Line() { StrokeThickness = 10, Stroke = Render.Gray };
                    Canvas.SetZIndex(wire, -1);
                    wire.X2 = wire.X1 = node.Position.X + Canvas.GetLeft(handledField) + node.Sprite.Width / 2;
                    wire.Y2 = wire.Y1 = node.Position.Y + Canvas.GetTop(handledField) + node.Sprite.Height / 2;
                    Field.Children.Add(wire);
                }
                else if (Keyboard.IsKeyDown(Key.LeftCtrl) && m_isWantToCreateNewElement && (node as Input)?.ConnectedOutput == null)
                {

                    if (!m_takedNodesToNewElements.Contains(node))
                    {
                        AddToNodes(node);
                        if (!takedElements.Contains(node.parentLE)) AddToTakedElements(node.parentLE);
                    }
                    else RemoveFromNodes(node);
                }
            }
            else if (TrySpriteToObject(e.Source, out Render rend) && !(e.Source is Button))
            {
                isWantToClick = true;
                if (Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    if (takedElements.Contains(rend))
                    {
                        if (m_isWantToCreateNewElement)
                        {
                            var nodes = new List<Node>();
                            foreach (Node n in m_takedNodesToNewElements)
                                if (n.parentLE == rend) nodes.Add(n);
                            foreach (Node n in nodes) RemoveFromNodes(n);
                        }
                        RemoveFromTakedElements(rend);
                    }
                    else AddToTakedElements(rend);
                }
                else if (!takedElements.Contains(rend) && !m_isWantToCreateNewElement)
                {
                    ClearTakedElements();
                    AddToTakedElements(rend);
                }
            }
            else if (!(e.Source is Button) && CreatePanel.Visibility != Visibility.Visible && !m_isWantToCreateNewElement)
            {
                ClearTakedElements();
            }
        }
        private void Field_MouseMove(object sender, MouseEventArgs e)
        {
            if (isWantToMoveField)
            {
                Canvas.SetTop(handledField, Canvas.GetTop(handledField) - m_oldWheelPosition.Y + e.GetPosition(Field).Y);
                Canvas.SetLeft(handledField, Canvas.GetLeft(handledField) - m_oldWheelPosition.X + e.GetPosition(Field).X);
                m_oldWheelPosition = e.GetPosition(Field);
            }
            else if (isLeftButtonDown && !(e.Source is Button))
            {
                if (wire != null)
                {
                    wire.X2 = e.GetPosition(Field).X - Math.Sign(wire.X2 - wire.X1);
                    wire.Y2 = e.GetPosition(Field).Y - Math.Sign(wire.Y2 - wire.Y1);
                }
                else if (takedElements.Count == 1)
                {
                    takedElements[0].Position = new Point(e.GetPosition(handledField).X - takedElements[0].Sprite.Width / 2, e.GetPosition(handledField).Y - takedElements[0].Sprite.Height / 2);
                }
                else if (takedElements.Count != 0 && TrySpriteToObject(e.Source, out Render takedElement) && takedElements.Contains(takedElement))
                {
                    Vector posto = e.GetPosition(handledField) - takedElement.Position - new Vector(takedElement.Sprite.Width / 2, takedElement.Sprite.Height / 2);
                    foreach (Render takedElem in takedElements)
                        takedElem.Position += posto;
                }
            }
            isWantToClick = false;
        }
        private void Field_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (TrySpriteToObject(e.Source, out Node node))
            {
                if (takedNode is Input input)
                {
                    if (node is Output && input.ConnectedOutput != node)
                    {
                        input.Disconnect(input.ConnectedOutput);
                        input.Connect(node);
                        RemoveFromNodes(input);
                    }
                    else if (node == input.ConnectedOutput) input.Disconnect(input.ConnectedOutput);
                    else if (takedNode == node) input.Disconnect(input.ConnectedOutput);
                }
                else if (takedNode is Output output && node is Input)
                {
                    if (!output.Inputs.Contains(node))
                    {
                        output.Connect(node);
                        RemoveFromNodes(node);
                    }
                    else output.Disconnect(node);
                }
            }
            else if (TrySpriteToObject(e.Source, out ButtonNode but) && isWantToClick && !Keyboard.IsKeyDown(Key.LeftCtrl)) but.Click();

            Field.Children.Remove(wire);
            wire = null;
            isLeftButtonDown = false;
            takedNode = null;
        }

        private void Field_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isWantToMoveField = e.MiddleButton == MouseButtonState.Pressed;
            if (e.ChangedButton == MouseButton.Middle)
                m_oldWheelPosition = e.GetPosition(Field);
            if (e.ChangedButton == MouseButton.Right)
            {
                if (TrySpriteToObject(e.Source, out Render temp) && !(temp is Node) && takedElements.Count <= 1)
                {
                    ClearTakedElements();
                    AddToTakedElements(temp);
                }
                if (!(temp is Node) && !(e.Source is TextBox) && e.RightButton == MouseButtonState.Released)
                {
                    MoreAction.Visibility = Visibility.Visible;
                    Canvas.SetLeft(MoreAction, e.GetPosition(MoreActionField).X);
                    Canvas.SetTop(MoreAction, e.GetPosition(MoreActionField).Y);
                }

                if (m_isWantToCreateNewElement && temp is Node node && m_takedNodesToNewElements.Contains(node))
                {
                    takedNodeForRename = node;
                    CanvasSetNameAndSymbolForNode.Visibility = Visibility.Visible;
                    Canvas.SetLeft(CanvasSetNameAndSymbolForNode, e.GetPosition(MoreActionField).X);
                    Canvas.SetTop(CanvasSetNameAndSymbolForNode, e.GetPosition(MoreActionField).Y);
                }
            }

        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                string menuName = menuItem.Header.ToString();
                switch (menuName)
                {
                    case "Clear":
                        handledField.Children.Clear();
                        break;
                    case "Delete button":
                        ButtonSpawn.isWantToDeleteButton = true;
                        CancelCreate();
                        buttonToCancelDelete.Visibility = Visibility.Visible;
                        break;
                    case "Create new element":
                        m_isWantToCreateNewElement = true;
                        Button_CancelDelete(null, null);
                        NewElementPanel.Visibility = Visibility.Visible;
                        break;
                }
            }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button but)
            {
                string text = but.Content.ToString();
                switch (text)
                {
                    case "Add":
                        int outCount = 0, inpCount = 0;
                        foreach (Node node in m_takedNodesToNewElements)
                        {
                            if (node is Input) inpCount++;
                            if (node is Output) outCount++;
                        }
                        if (inpCount == 0 && outCount == 0) textInfo.Text = "No nodes selected";
                        else if (inpCount == 0) textInfo.Text = "No inputs selected";
                        else if (outCount == 0) textInfo.Text = "No outputs selected";
                        else if (takedElements.Count() <= 1) textInfo.Text = "Less than two items selected";
                        else ActivateCreator();
                        break;
                    case "Cancel":
                        if (CreatePanel.Visibility == Visibility.Visible) CreatePanel.Visibility = Visibility.Hidden;
                        else
                        {
                            m_isWantToCreateNewElement = false;
                            ClearAllNodes();
                            NewElementPanel.Visibility = Visibility.Hidden;
                        }
                        break;
                }
            }
        }
        private void ActivateCreator()
        {
            canvasForSample.Children.Clear();
            CreatePanel.Visibility = Visibility.Visible;
            Render.Field = canvasForSample;

            var temp = CreateNewElement("", "");
            canvasForSample.RenderTransform = new ScaleTransform(200 / temp.Sprite.Height, 200 / temp.Sprite.Height);
            temp.Position = new Point(-temp.Sprite.Width / 2, -temp.Sprite.Height / 2);
            temp.AddToField();
            Render.Field = handledField;
            temp.Sprite.IsEnabled = false;
            foreach (Input inp in temp.Inputs)
            {
                inp.Sprite.IsEnabled = false;

            }
            foreach (Output outp in temp.Outputs)
            {

                outp.Sprite.IsEnabled = false;
            }

        }
        private void CreateClick(object sender, RoutedEventArgs e)
        {
            if (nameText.Text == "") textInfo.Text = "Name not set";
            else if (symbolsText.Text == "") textInfo.Text = "Symbols not set";
            else
            {
                var res = CreateNewElement(nameText.Text, symbolsText.Text);
                ClearAllNodes();
                res.AddToField();
                new ButtonSpawn(res.Copy());
                foreach (Render elem in takedElements) elem.Remove();
                CreatePanel.Visibility = Visibility.Hidden;
            }
        }
        private LogicalElement CreateNewElement(string name, string symbols)
        {
            LogicalElement[] logicalElements = (from elem in takedElements where elem is LogicalElement select (LogicalElement)elem).ToArray();
            List<Input> inputs = new List<Input>();
            List<Output> outputs = new List<Output>();
            foreach (Node node in m_takedNodesToNewElements)
            {
                if (node is Input inp) inputs.Add(inp);
                else if (node is Output outp) outputs.Add(outp);
            }
            Point position = logicalElements[0].Position;
            return new LogicalGroup(logicalElements, inputs.ToArray(), outputs.ToArray(), position, name, symbols);
        }

        private void CancelCreate()
        {
            m_isWantToCreateNewElement = false;
            ClearAllNodes();
            CreatePanel.Visibility = Visibility.Hidden;
            NewElementPanel.Visibility = Visibility.Hidden;
        }



        private void Field_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            k_Size += e.Delta > 0 ? 0.1 * k_Size : -0.1 * k_Size;
            k_Size = k_Size > 3.7 ? 3.7 : k_Size < 0.3 ? 0.3 : k_Size;
            transformSxaleText.Text = $"{(int)(k_Size * 10)}0%";
            Field.RenderTransform = new ScaleTransform(k_Size, k_Size);
        }
        private void DeleteButtonClick(object sender, RoutedEventArgs e)
        {
            if (e.Source is Button but)
            {
                if (but.Content.ToString() == "Delete")
                    ButtonSpawn.Remove(ButtonSpawn.buttonToDelete);
                DeletePanel.Visibility = Visibility.Hidden;
            }
        }

        private void Button_CancelDelete(object sender, RoutedEventArgs e)
        {
            ButtonSpawn.isWantToDeleteButton = false;
            buttonToCancelDelete.Visibility = Visibility.Hidden;
            DeletePanel.Visibility = Visibility.Hidden;
        }

        private void Button_SetParamsForNode(object sender, RoutedEventArgs e)
        {
            takedNodeForRename.SetName(NodeNameTextBlock.Text);
            takedNodeForRename.SetSymbol((NodeType)ComboBoxTakedSymbol.SelectedIndex);
            takedNodeForRename = null;
            CanvasSetNameAndSymbolForNode.Visibility = Visibility.Hidden;
        }

        private void Button_MoreActionClick(object sender, RoutedEventArgs e)
        {
            switch ((e.Source as Button).Content.ToString())
            {
                case "Remove":
                    takedElements.ForEach(elem => elem.Remove());
                    ClearTakedElements();
                    break;
                case "Copy":
                    RenderBuffer = (from elem in takedElements select elem.Copy()).ToArray();
                    ClearTakedElements();
                    break;
                case "Paste":
                    if ((RenderBuffer?.Length ?? 0) != 0)
                    {
                        ClearTakedElements();
                        Vector vector = new Vector(Canvas.GetLeft(MoreAction) - RenderBuffer[0].Position.X - Canvas.GetLeft(handledField),
                            Canvas.GetTop(MoreAction) - RenderBuffer[0].Position.Y - Canvas.GetTop(handledField));
                        foreach (Render rend in (from elem in RenderBuffer select elem.Copy()))
                        {
                            rend.AddToField();
                            rend.Position += vector;
                            AddToTakedElements(rend);
                        }
                    }
                    break;
                case "FlipX":
                    takedElements.ForEach(elem => (elem as LogicalElement)?.FlipX());
                    break;
                default:
                    break;
            }
            MoreAction.Visibility = Visibility.Hidden;
        }
    }
}
