using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;

namespace CombatSystemSystem
{
    public class MoveDataPreviewerEditorWindow2D : EditorWindow
    {
        VisualElement _editorPanel, _dataSelectionPanel, _scrubbingPannel;
        ObjectField _fld_MoveHandler;
        ObjectField _fld_GrabVictim;

        [SerializeField] MoveHandler2D _lastHandler;

        Vector3 _targetPosition;
        Quaternion _targetRotation;
        Vector3 _targetScale;

        MoveData.AnimationData _currentAnimationData;
        MoveData.HitboxData.SuccessfulGrabData _currentGrabData;
        MovesetExtender2D _currentMovesetExtender;

        float _aniFrameFloat = 0;
        int _aniFrameInt { get { return Mathf.RoundToInt(_aniFrameFloat); } }
        bool _isPlaying;

        [MenuItem("Window/CombatSystemSystem/2D Move Data Previewer")]
        public static void ShowExample()
        {
            MoveDataPreviewerEditorWindow2D wnd = GetWindow<MoveDataPreviewerEditorWindow2D>();
            wnd.titleContent = new GUIContent("2D Move Data Previewer");
        }

        private void OnEnable()
        {
            _editorPanel = _editorPanel ?? new VisualElement();
            _dataSelectionPanel = _dataSelectionPanel ?? new VisualElement();
            _scrubbingPannel = _scrubbingPannel ?? new VisualElement();

            AssemblyReloadEvents.beforeAssemblyReload += Reload;
            SceneView.duringSceneGui += OnSceneGUI;
        }
        private void OnDisable()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= Reload;
            SceneView.duringSceneGui -= OnSceneGUI;
        }
        void Reload()
        {
            StopAnimation();
            _dataSelectionPanel.Clear();
            _currentAnimationData = default;
            _currentMovesetExtender = null;
        }

        private void OnDestroy()
        {
            if (AnimationMode.InAnimationMode())
            {
                StopAnimation();
            }
        }

        private void Update()
        {
            if (_isPlaying)
            {
                bool isGrab = false;

                if (_currentAnimationData.animation == null)
                {
                    if (_currentGrabData.attackerGrabAnimation == null)
                    {
                        _isPlaying = false;
                        return;
                    }
                    else
                    {
                        _aniFrameFloat += Time.deltaTime * _currentGrabData.attackerGrabAnimation.frameRate;

                        if (_aniFrameFloat > _currentGrabData.attackerGrabAnimation.frameRate * _currentGrabData.attackerGrabAnimation.length)
                        {
                            _aniFrameFloat = 0;
                        }

                        isGrab = true;
                    }
                }
                else
                {
                    _aniFrameFloat += Time.deltaTime * _currentAnimationData.animation.frameRate;

                    if (_aniFrameFloat > _currentAnimationData.animation.frameRate * _currentAnimationData.animation.length)
                    {
                        _aniFrameFloat = 0;
                    }
                }
                

                SetAnimationTime(_aniFrameFloat, isGrab);
            }
        }

        private void OnInspectorUpdate()
        {
            if (_isPlaying)
            {
                if (_currentAnimationData.animation == null)
                {
                    if (_currentGrabData.attackerGrabAnimation != null) AnimationDataUpdated(_currentGrabData);
                }
                else AnimationDataUpdated(_currentAnimationData);
            }
        }

        private void OnSelectionChange()
        {
            if (_fld_MoveHandler.value == null)
            {
                if (Selection.gameObjects.Length > 0)
                {
                    MoveHandler2D success;

                    foreach (GameObject obj in Selection.gameObjects)
                    {
                        if (obj.TryGetComponent(out success))
                        {
                            _fld_MoveHandler.SetValueWithoutNotify(success);
                            MoveHandlerUpdated(success);
                            break;
                        }
                    }
                }
            }
            else
            {
                if (_currentGrabData.attackerGrabAnimation != null)
                {
                    if (Selection.gameObjects.Length > 0)
                    {
                        Animator success;

                        foreach (GameObject obj in Selection.gameObjects)
                        {
                            if (obj.TryGetComponent(out success) && obj != (_fld_MoveHandler.value as MoveHandler2D).gameObject)
                            {
                                _fld_GrabVictim.SetValueWithoutNotify(success);
                                break;
                            }
                        }
                    }
                }
            }
        }

        void OnSceneGUI(SceneView view)
        {
            if (_currentAnimationData.animation != null && AnimationMode.InAnimationMode())
            {
                Handles.color = new Color(0,1,1,.2f);

                Transform objTransform = _lastHandler.animationHandler.getAnimator.gameObject.transform;
                

                for (int i = 0; i < _currentAnimationData.blocks.Length; i++)
                {
                    bool shouldBeOn = _currentAnimationData.blocks[i].frameStart <= _aniFrameInt
                        && _aniFrameInt <= _currentAnimationData.blocks[i].frameEnd;

                    if (shouldBeOn)
                    {
                        Handles.DrawSolidArc(objTransform.position, Vector3.up, objTransform.forward, _currentAnimationData.blocks[i].coverageAngle / 2, 1);
                        Handles.DrawSolidArc(objTransform.position, Vector3.up, objTransform.forward, -_currentAnimationData.blocks[i].coverageAngle / 2, 1);
                    }
                }
            }

            if (_currentGrabData.damageInstances != null && _currentGrabData.damageInstances.Length != 0 && AnimationMode.InAnimationMode())
            {
                Handles.color = new Color(1, 0, 0, .2f);

                Transform objTransform = _lastHandler.animationHandler.getAnimator.gameObject.transform;

                for (int i = 0; i < _currentGrabData.damageInstances.Length; i++)
                {
                    bool shouldBeOn = _currentGrabData.damageInstances[i].frame == _aniFrameInt;

                    if (shouldBeOn)
                    {
                        Handles.DrawSolidDisc(objTransform.position + Vector3.up, Vector3.up, 1);
                    }
                }
            }
        }


        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            _fld_MoveHandler = new ObjectField("Move Handler");
            _fld_MoveHandler.objectType = typeof(MoveHandler2D);
            _fld_MoveHandler.RegisterValueChangedCallback(evt => { MoveHandlerUpdated(evt.newValue as MoveHandler2D); });

            _fld_GrabVictim = new ObjectField("Grab Victim");
            _fld_GrabVictim.objectType = typeof(Animator);

            _editorPanel = new VisualElement();
            _dataSelectionPanel = new VisualElement();
            _scrubbingPannel = new VisualElement();

            root.Add(_fld_MoveHandler);
            root.Add(_editorPanel);

            if (_fld_MoveHandler.value == null)
            {
                if (Selection.gameObjects.Length > 0)
                {
                    MoveHandler2D success;

                    foreach (GameObject obj in Selection.gameObjects)
                    {
                        if (obj.TryGetComponent(out success))
                        {
                            _fld_MoveHandler.SetValueWithoutNotify(success);
                            MoveHandlerUpdated(success);
                            break;
                        }
                    }
                }
                
            }
            else
            {
                MoveHandlerUpdated(_fld_MoveHandler.value as MoveHandler2D);
            }



            Button playButton = new Button();
            playButton.text = "Play/Pause";
            playButton.clicked += () => { _isPlaying = !_isPlaying; };

            var playbackRow = new VisualElement();
            playbackRow.style.flexDirection = FlexDirection.Row;
            playbackRow.style.justifyContent = Justify.Center;


            var resetButton = new Button();
            resetButton.text = "Reset Pose";
            resetButton.clicked += StopAnimation;


            playbackRow.Add(playButton);
            root.Add(playbackRow);
            root.Add(resetButton);
        }

        void MoveHandlerUpdated(MoveHandler2D target)
        {
            _editorPanel.Clear();
            _dataSelectionPanel.Clear();
            _scrubbingPannel.Clear();

            if (target == null)
            {
                _editorPanel.Add(new Label("Please Select a MoveHandler2D"));
                return;
            }
            
            _lastHandler = target;

            var allConnectedMoveDatas = new List<MoveExtenderPair>();
            
            for (int i = 0; i < target.movesetExtenders.Length; i++)
            {
                for (int j = 0; j < target.movesetExtenders[i].moveset.moves.Length; j++)
                {
                    allConnectedMoveDatas.Add(new MoveExtenderPair(target.movesetExtenders[i].moveset.moves[j], target.movesetExtenders[i]));
                }
            }

            var spacer = new VisualElement();
            spacer.style.height = 12;
            var container = new Box();
            var splitView = new TwoPaneSplitView(0, 220, TwoPaneSplitViewOrientation.Horizontal);

            var listPane = new ListView();
            listPane.makeItem = () => new Label();
            listPane.bindItem = (item, index) => { (item as Label).text = allConnectedMoveDatas[index].move.name; };
            listPane.itemsSource = allConnectedMoveDatas;
            listPane.selectedIndex = 0;

            listPane.selectionChanged += (selection) => 
            { 
                var enumerator = selection.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    MoveDataUpdated(enumerator.Current as MoveExtenderPair);

                    Selection.SetActiveObjectWithContext((enumerator.Current as MoveExtenderPair).move, null);
                }
            };

            _editorPanel.Add(spacer);
            _editorPanel.Add(container);
            container.Add(splitView);
            splitView.Add(listPane);
            splitView.Add(_dataSelectionPanel);

            if (allConnectedMoveDatas.Count > 0)
            {
                MoveDataUpdated(allConnectedMoveDatas[0]);
            }
        }

        void MoveDataUpdated(MoveExtenderPair target)
        {
            _dataSelectionPanel.Clear();
            _scrubbingPannel.Clear();

            if (target == null || target.move == null || target.extender == null)
            {
                _dataSelectionPanel.Add(new Label("Invalid Move Data"));
                return;
            }

            _currentMovesetExtender = target.extender;

            var fld_dropdown = new DropdownField("Select Animation Data");

            fld_dropdown.value = "Start Animation";
            AnimationDataUpdated(target.move.baseAnimation);

            if (target.move.canHoldMove)
            {
                fld_dropdown.choices.Add("Start Animation");
                fld_dropdown.choices.Add("Hold Animation");
                fld_dropdown.choices.Add("End Animation");

                fld_dropdown.RegisterValueChangedCallback((evt) =>
                {
                    switch (evt.newValue)
                    {
                        case "Hold Animation": AnimationDataUpdated(target.move.holdAnimation); break;
                        case "End Animation": AnimationDataUpdated(target.move.endAnimation); break;
                        default: AnimationDataUpdated(target.move.baseAnimation); break;
                    }
                });

                _dataSelectionPanel.Add(fld_dropdown);
            }
            else if (target.move.isGrab)
            {
                fld_dropdown.choices.Add("Start Animation");
                
                for (int i = 0; i < target.move.baseAnimation.hitboxes.Length; i++)
                {
                    if (target.move.baseAnimation.hitboxes[i].successfulGrabData.attackerGrabAnimation != null) // If this hitbox is a grab hitbox
                    {
                        MoveData.HitboxData.SuccessfulGrabData grabData = target.move.baseAnimation.hitboxes[i].successfulGrabData;

                        fld_dropdown.choices.Add(grabData.attackerGrabAnimation.name);

                        fld_dropdown.RegisterValueChangedCallback(evt => 
                        {
                            if (evt.newValue == grabData.attackerGrabAnimation.name)
                            {
                                AnimationDataUpdated(grabData);
                            }
                        });
                    }
                }

                _dataSelectionPanel.Add(fld_dropdown);
            }

            _dataSelectionPanel.Add(_scrubbingPannel);
        }

        void AnimationDataUpdated(MoveData.AnimationData target)
        {
            if (!_isPlaying) StopAnimation();

            _currentAnimationData = target;
            _currentGrabData = default;

            _scrubbingPannel.Clear();

            var container = new Box();

            float padding = 10;

            container.style.paddingBottom = padding;
            container.style.paddingTop = padding;
            container.style.paddingLeft = padding;
            container.style.paddingRight = padding;

            _scrubbingPannel.Add(container);


            if (target.animation == null)
            {
                container.Add(new Label("This function requires the Animation Data for timing calculations. Please add an animation to the move, even if it's empty."));

                return;
            }


            var scrubSlider = new Slider(0, Mathf.RoundToInt(target.animation.length * target.animation.frameRate));
            if (_isPlaying) scrubSlider.SetValueWithoutNotify(_aniFrameFloat);

            scrubSlider.RegisterValueChangedCallback(evt =>
            {
                _aniFrameFloat = evt.newValue;

                SetAnimationTime(_aniFrameFloat);
            });


            container.Add(scrubSlider);
        }

        void AnimationDataUpdated(MoveData.HitboxData.SuccessfulGrabData target)
        {
            if (!_isPlaying) StopAnimation();

            _currentAnimationData = default;
            _currentGrabData = target;

            _scrubbingPannel.Clear();

            var container = new Box();

            float padding = 10;

            container.style.paddingBottom = padding;
            container.style.paddingTop = padding;
            container.style.paddingLeft = padding;
            container.style.paddingRight = padding;

            _scrubbingPannel.Add(container);


            if (target.attackerGrabAnimation == null)
            {
                container.Add(new Label("This function requires the Animation Data for timing calculations. Please add an animation to the move, even if it's empty."));

                return;
            }


            var scrubSlider = new Slider(0, Mathf.RoundToInt(target.attackerGrabAnimation.length * target.attackerGrabAnimation.frameRate));
            if (_isPlaying) scrubSlider.SetValueWithoutNotify(_aniFrameFloat);

            scrubSlider.RegisterValueChangedCallback(evt =>
            {
                _aniFrameFloat = evt.newValue;

                SetAnimationTime(_aniFrameFloat, true);
            });


            container.Add(scrubSlider);
            container.Add(_fld_GrabVictim);
        }





        void SetAnimationTime(float frameTime, bool isGrab = false)
        {
            StartAnimation();

            AnimationClip animation = isGrab ? _currentGrabData.attackerGrabAnimation : _currentAnimationData.animation;


            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(_lastHandler.animationHandler.getAnimator.gameObject, animation, frameTime / animation.frameRate);

            _lastHandler.transform.position = _targetPosition;
            _lastHandler.transform.rotation = _targetRotation;
            _lastHandler.transform.localScale = _targetScale;

            if (isGrab && _fld_GrabVictim.value != null)
            {
                GameObject victim = (_fld_GrabVictim.value as Animator).gameObject;

                AnimationMode.SampleAnimationClip(victim, _currentGrabData.victimGrabAnimation, frameTime / _currentGrabData.victimGrabAnimation.frameRate);

                victim.transform.position = _currentMovesetExtender.positionDictionary.transforms[_currentGrabData.teleportPositionDictionaryReference].position;
                victim.transform.rotation = _currentMovesetExtender.positionDictionary.transforms[_currentGrabData.teleportPositionDictionaryReference].rotation;
            }
            else
            {
                for (int i = 0; i < _currentAnimationData.hitboxes.Length; i++)
                {
                    bool shouldBeOn = _currentAnimationData.hitboxes[i].frameStart <= _aniFrameInt
                        && _aniFrameInt <= _currentAnimationData.hitboxes[i].frameEnd;

                    for (int j = 0; j < _currentAnimationData.hitboxes[i].colliderDictionaryReferences.Length; j++)
                    {
                        _currentMovesetExtender.colliderDictionary.colliders[_currentAnimationData.hitboxes[i].colliderDictionaryReferences[j]].collider.gameObject.SetActive(shouldBeOn);
                    }
                }
            }
            AnimationMode.EndSampling();
        }

        void StartAnimation()
        {
            _targetPosition = _lastHandler.transform.position;
            _targetRotation = _lastHandler.transform.rotation;
            _targetScale = _lastHandler.transform.localScale;


            AnimationMode.StartAnimationMode();

            if (_currentMovesetExtender == null) return;

            for (int i = 0; i < _currentMovesetExtender.colliderDictionary.colliders.Length; i++)
            {
                _currentMovesetExtender.colliderDictionary.colliders[i].collider.gameObject.SetActive(false);
            }
        }

        void StopAnimation()
        {
            _isPlaying = false;

            if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();

            if (_currentMovesetExtender == null) return;

            for (int i = 0; i < _currentMovesetExtender.colliderDictionary.colliders.Length; i++)
            {
                _currentMovesetExtender.colliderDictionary.colliders[i].collider.gameObject.SetActive(true);
            }
        }


        class MoveExtenderPair
        {
            public MoveData move;
            public MovesetExtender2D extender;

            public MoveExtenderPair(MoveData m, MovesetExtender2D e)
            {
                move = m;
                extender = e;
            }
        }

    }
}

