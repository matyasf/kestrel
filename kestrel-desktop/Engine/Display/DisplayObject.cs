using System;
using System.Collections.Generic;
using Engine.Geom;
using Veldrid;
using Point = Engine.Geom.Point;
using Rectangle = Engine.Geom.Rectangle;

namespace Engine.Display
{
    /// <summary>
    /// DisplayObject is the base class for every renderable object.
    /// </summary>
    public abstract class DisplayObject
    {
        public delegate void EnterFrame(DisplayObject target, float elapsedTimeSecs);
        public delegate void UIChange(DisplayObject target);

        public event EnterFrame EnterFrameEvent;
        public event UIChange AddedToStage;
        public event UIChange RemovedFromStage;
        public bool Visible = true;

        private float _pivotX;
        private float _pivotY;
        private float _scaleX = 1.0f;
        private float _scaleY = 1.0f;
        
        /// <summary>
        /// The GPU resource set for this object. Its the same object for objects with the same image.
        /// </summary>
        internal ResourceSet ResSet;
        private RenderState _renderState;
        private QuadVertex _gpuVertex;
        private readonly Matrix2D _transformationMatrix;
        private bool _transformationMatrixChanged = true;
        
        public DisplayObject()
        {
            _gpuVertex.Tint = RgbaByte.White;
            _transformationMatrix = Matrix2D.Create();
        }
        
        internal ref QuadVertex GetGpuVertex()
        {
            _gpuVertex.Position.X = _renderState.ModelviewMatrix.Tx;
            _gpuVertex.Position.Y = _renderState.ModelviewMatrix.Ty;
            _gpuVertex.Size.X = OriginalWidth * _renderState.ScaleX;
            _gpuVertex.Size.Y = OriginalHeight * _renderState.ScaleY;
            _gpuVertex.Rotation = _renderState.ModelviewMatrix.Rotation;
            return ref _gpuVertex;
        }

        protected bool IsOnStageProperty;
        /// <summary>
        /// Whether this instance is on the Stage. If something is not on the Stage, it will not render.
        /// </summary>
        public bool IsOnStage
        {
            get => IsOnStageProperty;
            internal set
            {
                if (value != IsOnStageProperty)
                {
                    IsOnStageProperty = value;
                    if (value)
                    {
                        AddedToStage?.Invoke(this);
                    }
                    else
                    {
                        RemovedFromStage?.Invoke(this);
                    }
                }
            }
        }
        
        public void RemoveFromParent()
        {
            if (Parent != null)
            {
                Parent.RemoveChild(this);
                Parent = null;
                IsOnStage = false;
            }
        }

        public virtual void Render(float elapsedTimeSecs)
        {
            if (IsOnStageProperty)
            {
                InvokeEnterFrameEvent(elapsedTimeSecs);
            }
            if (Visible)
            {
                _renderState = KestrelApp.Renderer.PushRenderState(1.0f, TransformationMatrix, _scaleX, _scaleY);
                var bounds = GetBounds();
                if (bounds.Width > 0 && bounds.Height > 0)
                {
                    KestrelApp.Renderer.AddToRenderQueue(this);
                }
                foreach (var child in Children)
                {
                    child.Render(elapsedTimeSecs);
                }
                KestrelApp.Renderer.PopRenderState();
            }
        }

        public virtual DisplayObject Parent { get; internal set; }

        private float _x;
        public virtual float X
        {
            get => _x;
            set
            {
                if (value == _x)
                {
                    return;
                }
                _transformationMatrixChanged = true;
                _x = value;
            }
        }

        private float _y;
        public virtual float Y
        {
            get => _y;
            set
            {
                if (value == _y)
                {
                    return;
                }
                _transformationMatrixChanged = true;
                _y = value;
            }
        }

        protected float OriginalWidth;
        /// <summary>
        /// The width of the object without transformations.
        /// </summary>
        public virtual float Width => OriginalWidth;

        protected float OriginalHeight;
        /// <summary>
        /// The height of the object without transformations.
        /// </summary>
        public virtual float Height => OriginalHeight;

        /// <summary>
        /// The width of the object after scaling
        /// </summary>
        public virtual float WidthScaled
        {
            get => OriginalWidth * _scaleX;
            set => ScaleX = value / OriginalWidth;
        }

        /// <summary>
        /// The height of the object after scaling
        /// </summary>
        public virtual float HeightScaled
        {
            get => OriginalHeight * _scaleY;
            set => ScaleY = value / OriginalHeight;
        }
        
        private float _rotation;
        /// <summary>
        /// Rotation in Radians.
        /// </summary>
        public virtual float Rotation
        {
            get => _rotation;
            set
            {
                if (value == _rotation)
                {
                    return;
                }
                // move to equivalent value in range [0 deg, 360 deg]
                value = value % (float)(2.0f * Math.PI);
                // move to [-180 deg, +180 deg]
                if (value < -Math.PI)
                {
                    value += 2.0f * (float)Math.PI;
                }
                else if (value > Math.PI)
                {
                    value -= 2.0f * (float)Math.PI;
                }
                _transformationMatrixChanged = true;
                _rotation = value;
            }
        }

        public RgbaByte Tint
        {
            get => _gpuVertex.Tint;
            set => _gpuVertex.Tint = value;
        }

        /// <summary>
        /// Returns the object that is found topmost on a point in local coordinates, or null if the test fails.
        /// </summary>
        public virtual DisplayObject HitTest(Point p)
        {
            if (!Visible)
            {
                return null;
            }
            for (var i = Children.Count - 1; i >= 0; --i) // front to back!
            {
                DisplayObject child = Children[i];
                if (child.Visible)
                {
                    Matrix2D transformationMatrix = Matrix2D.Create();
                    transformationMatrix.CopyFromMatrix(child.TransformationMatrix);
                    transformationMatrix.Invert();

                    Point transformedPoint = transformationMatrix.TransformPoint(p);
                    DisplayObject target = child.HitTest(transformedPoint);
                    if (target != null)
                    {
                        return target;
                    }
                }
            }
            return null;
        }

        internal void InvokeEnterFrameEvent(float elapsedTimeSecs)
        {
            EnterFrameEvent?.Invoke(this, elapsedTimeSecs);
        }
        
        /// <summary>
        /// Returns the bounds of this object after transformations
        /// </summary>
        public virtual Rectangle GetBounds()
        {
            return GetBounds(Parent);
        }
        
        public virtual Rectangle GetBounds(DisplayObject targetSpace)
        {
            Rectangle outRect = Rectangle.Create();
            if (targetSpace == this) // Optimization
            {
                outRect.Width = OriginalWidth;
                outRect.Height = OriginalHeight;
            }
            else if (targetSpace == Parent && !IsRotated) // Optimization
            {
                outRect = Rectangle.Create(_x - _pivotX * _scaleX,
                    _y - _pivotY * _scaleY,
                    OriginalWidth * _scaleX,
                    OriginalHeight * _scaleY);
                if (_scaleX < 0.0f)
                {
                    outRect.Width *= -1.0f;
                    outRect.X -= outRect.Width;
                }
                if (_scaleY < 0.0f)
                {
                    outRect.Height *= -1.0f;
                    outRect.Top -= outRect.Height;
                }
            }
            else
            {
                outRect.Width = OriginalWidth;
                outRect.Height = OriginalHeight;
                Matrix2D sMatrix = GetTransformationMatrix(targetSpace);
                outRect = outRect.GetBounds(sMatrix);
            }
            return outRect;
        }

        public virtual Rectangle GetBoundsWithChildren()
        {
            return GetBoundsWithChildren(Parent);
        }
        public virtual Rectangle GetBoundsWithChildren(DisplayObject targetSpace)
        {
            var ownBounds = GetBounds(targetSpace);
            float minX = ownBounds.X, maxX = ownBounds.Right;
            float minY = ownBounds.Y, maxY = ownBounds.Bottom;
            foreach (DisplayObject child in Children)
            {
                Rectangle childBounds = child.GetBoundsWithChildren(targetSpace);
                minX = Math.Min(minX, childBounds.X);
                maxX = Math.Max(maxX, childBounds.X + childBounds.Width);
                minY = Math.Min(minY, childBounds.Top);
                maxY = Math.Max(maxY, childBounds.Top + childBounds.Height);
            }
            return Rectangle.Create(minX, minY, maxX - minX, maxY - minY);
        }
        
        /// <summary>
        /// The transformation matrix of the object relative to its parent.
        /// <returns>CAUTION: not a copy, but the actual object!</returns>
        /// </summary>
        public Matrix2D TransformationMatrix
        {
            get
            {
                if (!_transformationMatrixChanged)
                {
                    return _transformationMatrix;
                }
                _transformationMatrix.Identity();
                _transformationMatrix.Scale(_scaleX, _scaleY);
                _transformationMatrix.Rotate(_rotation);
                _transformationMatrix.Translate(_x, _y);

                if (_pivotX != 0.0f || _pivotY != 0.0f)
                {
                    // prepend pivot transformation
                    _transformationMatrix.Tx = _x - _transformationMatrix.A * _pivotX
                                                  - _transformationMatrix.C * _pivotY;
                    _transformationMatrix.Ty = _y - _transformationMatrix.B * _pivotX
                                                  - _transformationMatrix.D * _pivotY;
                }
                _transformationMatrixChanged = false;
                return _transformationMatrix;
            }
        }
        
        /// <summary>
        /// Creates a matrix that represents the transformation from the local coordinate system to another.
        /// </summary>
        public Matrix2D GetTransformationMatrix(DisplayObject targetSpace)
        {
            DisplayObject currentObject;
            Matrix2D outMatrix = Matrix2D.Create();
            outMatrix.Identity();
            if (targetSpace == this)
            {
                return outMatrix;
            }
            if (targetSpace == Parent || (targetSpace == null && Parent == null))
            {
                outMatrix.CopyFromMatrix(TransformationMatrix);
                return outMatrix;
            }
            if (targetSpace == null || targetSpace == Root)
            {
                // if targetSpace 'null', we assume that we need it in the space of the Base object.
                // -> move up from this to base
                currentObject = this;
                while (currentObject != targetSpace)
                {
                    outMatrix.AppendMatrix(currentObject.TransformationMatrix);
                    currentObject = currentObject.Parent;
                }
                return outMatrix;
            }
            if (targetSpace.Parent == this)
            {
                outMatrix = targetSpace.GetTransformationMatrix(this);
                outMatrix.Invert();
                return outMatrix;
            }
            // targetSpace is not an ancestor
            // 1.: Find a common parent of this and the target coordinate space.
            var commonParent = FindCommonParent(this, targetSpace);

            // 2.: Move up from this to common parent
            currentObject = this;
            while (currentObject != commonParent)
            {
                outMatrix.AppendMatrix(currentObject.TransformationMatrix);
                currentObject = currentObject.Parent;
            }

            if (commonParent == targetSpace)
            {
                return outMatrix;
            }

            // 3.: Now move up from target until we reach the common parent
            var sHelperMatrix = Matrix2D.Create();
            sHelperMatrix.Identity();
            currentObject = targetSpace;
            while (currentObject != commonParent)
            {
                sHelperMatrix.AppendMatrix(currentObject.TransformationMatrix);
                currentObject = currentObject.Parent;
            }

            // 4.: Combine the two matrices
            sHelperMatrix.Invert();
            outMatrix.AppendMatrix(sHelperMatrix);

            return outMatrix;
        }
        
        private static readonly List<DisplayObject> CommonParentHelper = new List<DisplayObject>();
        private static DisplayObject FindCommonParent(DisplayObject object1, DisplayObject object2)
        {
            DisplayObject currentObject = object1;
            while (currentObject != null)
            {
                CommonParentHelper.Add(currentObject);
                currentObject = currentObject.Parent;
            }
            currentObject = object2;
            while (currentObject != null && CommonParentHelper.Contains(currentObject) == false)
            {
                currentObject = currentObject.Parent;
            }
            CommonParentHelper.Clear();
            if (currentObject != null)
            {
                return currentObject;
            }
            throw new ArgumentException("Object not connected to target");
        }
        
        /// <summary>
        /// Indicates if the object is rotated or skewed in any way.
        /// </summary>
        internal bool IsRotated => _rotation != 0.0;
        
        /// <summary>
        /// The topmost object in the display tree the object is part of.
        /// </summary>
        public DisplayObject Root
        {
            get
            {
                DisplayObject currentObject = this;
                while (currentObject.Parent != null)
                {
                    currentObject = currentObject.Parent;
                }
                return currentObject;
            }
        }

        public float ScaleX
        {
            get => _scaleX;
            set
            {
                if (value == _scaleX)
                {
                    return;
                }
                _transformationMatrixChanged = true;
                _scaleX = value;
            }
        }

        public float ScaleY
        {
            get => _scaleY;
            set
            {
                if (value == _scaleY)
                {
                    return;
                }
                _transformationMatrixChanged = true;
                _scaleY = value;
            }
        }

        public float PivotX
        {
            get => _pivotX;
            set
            {
                if (value == _pivotX)
                {
                    return;
                }
                _transformationMatrixChanged = true;
                _pivotX = value;
            }
        }

        public float PivotY
        {
            get => _pivotY;
            set
            {
                if (value == _pivotY)
                {
                    return;
                }
                _transformationMatrixChanged = true;
                _pivotY = value;
            }
        }

        protected readonly List<DisplayObject> Children = new List<DisplayObject>();
        
        public virtual void AddChild(DisplayObject child)
        {
            if (child.Parent != null)
            {
                child.RemoveFromParent();
            }
            Children.Add(child);
            if (IsOnStageProperty)
            {
                child.IsOnStage = true;
            }
            child.Parent = this;
        }

        public virtual void RemoveChild(DisplayObject child)
        {
            Children.Remove(child);
            child.IsOnStage = false;
            child.Parent = null;
        }
    }
}