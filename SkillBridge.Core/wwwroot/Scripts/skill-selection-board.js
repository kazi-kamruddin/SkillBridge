(function () {
    'use strict';

    function formatBytes(bytes) {
        if (bytes < 1024) return bytes + ' B';
        if (bytes < 1024 * 1024) return Math.max(1, Math.round(bytes / 1024)) + ' KB';
        return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
    }

    function initAvatarPicker(root) {
        var input = root.querySelector('[data-sb-avatar-input]');
        if (!input) return;

        var maxBytes = parseInt(input.getAttribute('data-sb-avatar-max-bytes') || String(3 * 1024 * 1024), 10);
        var errorEl = root.querySelector('[data-sb-avatar-error]');
        var previewWrap = root.querySelector('[data-sb-avatar-preview]');
        var previewSlot = root.querySelector('[data-sb-avatar-img-slot]');
        var previewImg = root.querySelector('[data-sb-avatar-img]');
        var previewName = root.querySelector('[data-sb-avatar-name]');
        var previewSize = root.querySelector('[data-sb-avatar-size]');
        var clearBtn = root.querySelector('[data-sb-avatar-clear]');
        var removeCheckbox = root.querySelector('input[name="RemoveProfileImage"]');
        var existingUrl = root.getAttribute('data-sb-existing-avatar') || '';
        var objectUrl = null;

        function setPreviewImageSource(src, altText) {
            if (!src) {
                if (previewImg && previewSlot && previewImg.parentNode === previewSlot && !existingUrl) {
                    previewSlot.removeChild(previewImg);
                    previewImg = null;
                } else if (previewImg) {
                    previewImg.removeAttribute('src');
                }
                return;
            }
            if (!previewImg && previewSlot) {
                previewImg = document.createElement('img');
                previewImg.className = 'sb-avatar-preview-img';
                previewImg.setAttribute('data-sb-avatar-img', '');
                previewImg.width = 64;
                previewImg.height = 64;
                previewSlot.appendChild(previewImg);
            }
            if (previewImg) {
                previewImg.alt = altText;
                previewImg.src = src;
            }
        }

        function revokePreviewUrl() {
            if (objectUrl) {
                URL.revokeObjectURL(objectUrl);
                objectUrl = null;
            }
        }

        function hideClientError() {
            if (!errorEl) return;
            errorEl.textContent = '';
            errorEl.hidden = true;
        }

        function showClientError(msg) {
            if (!errorEl) return;
            errorEl.textContent = msg;
            errorEl.hidden = false;
        }

        function restoreDefaultPreview() {
            revokePreviewUrl();
            if (!previewWrap) return;
            if (existingUrl) {
                setPreviewImageSource(existingUrl, 'Current profile photo');
                if (previewName) previewName.textContent = removeCheckbox && removeCheckbox.checked ? 'Photo will be removed on save' : 'Current profile photo';
                if (previewSize) previewSize.textContent = '';
                if (clearBtn) clearBtn.hidden = true;
                previewWrap.hidden = false;
                previewWrap.classList.toggle('is-marked-removed', !!(removeCheckbox && removeCheckbox.checked));
            } else {
                setPreviewImageSource('', '');
                if (previewName) previewName.textContent = '';
                if (previewSize) previewSize.textContent = '';
                if (clearBtn) clearBtn.hidden = true;
                previewWrap.hidden = true;
            }
        }

        restoreDefaultPreview();

        input.addEventListener('change', function () {
            hideClientError();
            var file = input.files && input.files[0];
            if (!file) {
                restoreDefaultPreview();
                return;
            }

            if (file.size > maxBytes) {
                showClientError(
                    file.name + ' is ' + formatBytes(file.size) +
                    ', which exceeds the ' + Math.round(maxBytes / (1024 * 1024)) +
                    ' MB limit. Choose a smaller JPEG, PNG, or WebP photo.'
                );
                input.value = '';
                restoreDefaultPreview();
                return;
            }

            var allowedTypes = ['image/jpeg', 'image/png', 'image/webp'];
            if (file.type && allowedTypes.indexOf(file.type.toLowerCase()) === -1) {
                showClientError('Unsupported image format (' + file.type + '). Please choose a JPEG, PNG, or WebP file.');
                input.value = '';
                restoreDefaultPreview();
                return;
            }

            if (removeCheckbox) removeCheckbox.checked = false;
            revokePreviewUrl();
            if (previewWrap) {
                objectUrl = URL.createObjectURL(file);
                setPreviewImageSource(objectUrl, 'Selected profile photo preview');
                if (previewName) previewName.textContent = file.name;
                if (previewSize) previewSize.textContent = formatBytes(file.size) + ' ready';
                if (clearBtn) clearBtn.hidden = false;
                previewWrap.classList.remove('is-marked-removed');
                previewWrap.hidden = false;
            }
        });

        if (clearBtn) {
            clearBtn.addEventListener('click', function () {
                input.value = '';
                hideClientError();
                restoreDefaultPreview();
            });
        }

        if (removeCheckbox) {
            removeCheckbox.addEventListener('change', function () {
                if (removeCheckbox.checked && input.files && input.files.length > 0) {
                    input.value = '';
                    hideClientError();
                }
                restoreDefaultPreview();
            });
        }
    }

    function initSkillBoard(board) {
        var stagesScript = board.querySelector('[data-sb-stages-json]');
        var verifiedScript = board.querySelector('[data-sb-verified-json]');
        var hiddenInputsWrap = board.querySelector('[data-sb-hidden-inputs]');
        var learnCountEl = board.querySelector('[data-sb-learn-count]');
        var knowCountEl = board.querySelector('[data-sb-know-count]');

        var skillStagesData = {};
        var verifiedLearningStages = {};
        try {
            skillStagesData = stagesScript ? JSON.parse(stagesScript.textContent || '{}') : {};
        } catch (e) { skillStagesData = {}; }
        try {
            verifiedLearningStages = verifiedScript ? JSON.parse(verifiedScript.textContent || '{}') : {};
        } catch (e) { verifiedLearningStages = {}; }

        var selectedLearn = new Set();
        var selectedKnow = {};
        var activeDrawerSkillId = null;
        var activeDrawerEl = null;

        // Hydrate initial state from server-rendered hidden inputs so state is always aligned.
        if (hiddenInputsWrap) {
            hiddenInputsWrap.querySelectorAll('input[name="SkillsToLearn"]').forEach(function (inp) {
                selectedLearn.add(String(inp.value));
            });
            hiddenInputsWrap.querySelectorAll('input[name$=".SkillId"]').forEach(function (inp) {
                var skillId = String(inp.value);
                var stageInput = hiddenInputsWrap.querySelector('input[data-skill-id="' + skillId + '"][name$=".KnownUpToStage"]');
                var stageVal = stageInput ? parseInt(stageInput.value, 10) : 1;
                selectedKnow[skillId] = isNaN(stageVal) ? 1 : stageVal;
            });
        }

        function syncHiddenInputs() {
            if (!hiddenInputsWrap) return;
            hiddenInputsWrap.innerHTML = '';

            selectedLearn.forEach(function (skillId) {
                var input = document.createElement('input');
                input.type = 'hidden';
                input.name = 'SkillsToLearn';
                input.value = skillId;
                hiddenInputsWrap.appendChild(input);
            });

            var idx = 0;
            Object.keys(selectedKnow).forEach(function (skillId) {
                var idInput = document.createElement('input');
                idInput.type = 'hidden';
                idInput.name = 'SkillsIKnow[' + idx + '].SkillId';
                idInput.value = skillId;
                idInput.setAttribute('data-skill-id', skillId);
                hiddenInputsWrap.appendChild(idInput);

                var stageInput = document.createElement('input');
                stageInput.type = 'hidden';
                stageInput.name = 'SkillsIKnow[' + idx + '].KnownUpToStage';
                stageInput.value = String(selectedKnow[skillId]);
                stageInput.setAttribute('data-skill-id', skillId);
                hiddenInputsWrap.appendChild(stageInput);

                idx++;
            });
        }

        function updateCounters() {
            if (learnCountEl) learnCountEl.textContent = selectedLearn.size + ' selected';
            if (knowCountEl) knowCountEl.textContent = Object.keys(selectedKnow).length + ' selected';
        }

        function formatStageSummary(stage, total) {
            if (stage <= 1) return 'Stage 1 of ' + total;
            return 'Stages 1\u2013' + stage + ' of ' + total;
        }

        function updateKnowCardVisuals(skillId) {
            var cell = board.querySelector('.sb-skill-know-cell[data-skill-id="' + skillId + '"]');
            if (!cell) return;
            var card = cell.querySelector('.sb-skill-know-card');
            var btn = cell.querySelector('.skill-know');
            var trigger = cell.querySelector('.sb-stage-trigger');
            var triggerText = cell.querySelector('.sb-stage-trigger-text');
            var verifiedHint = cell.querySelector('.sb-verified-hint');
            var isSelected = Object.prototype.hasOwnProperty.call(selectedKnow, skillId);
            var totalStages = btn ? parseInt(btn.getAttribute('data-total-stages') || '1', 10) : 1;
            var skillName = btn ? (btn.getAttribute('data-skill-name') || '') : '';

            cell.classList.toggle('is-selected', isSelected);
            if (card) card.classList.toggle('is-selected', isSelected);
            if (btn) {
                btn.classList.toggle('active', isSelected);
                btn.setAttribute('aria-pressed', isSelected ? 'true' : 'false');
            }
            if (trigger) {
                if (isSelected) {
                    var stage = selectedKnow[skillId] || 1;
                    var summary = formatStageSummary(stage, totalStages);
                    trigger.removeAttribute('hidden');
                    if (triggerText) triggerText.textContent = summary;
                    trigger.setAttribute('aria-label', 'Choose taught stages for ' + skillName + ' (' + summary + ')');
                    trigger.setAttribute('aria-expanded', activeDrawerSkillId === skillId ? 'true' : 'false');
                } else {
                    trigger.setAttribute('hidden', 'hidden');
                    trigger.setAttribute('aria-expanded', 'false');
                }
            }
            if (verifiedHint) {
                verifiedHint.hidden = isSelected;
            }
        }

        function closeStageDrawer(restoreFocus) {
            var prevSkillId = activeDrawerSkillId;
            if (activeDrawerEl && activeDrawerEl.parentNode) {
                activeDrawerEl.parentNode.removeChild(activeDrawerEl);
            }
            activeDrawerEl = null;
            activeDrawerSkillId = null;

            if (prevSkillId) {
                updateKnowCardVisuals(prevSkillId);
                if (restoreFocus) {
                    var prevTrigger = board.querySelector('.sb-skill-know-cell[data-skill-id="' + prevSkillId + '"] .sb-stage-trigger');
                    if (prevTrigger && !prevTrigger.hasAttribute('hidden')) {
                        prevTrigger.focus();
                    }
                }
            }
        }

        function findRowAnchorCell(cell, grid) {
            var cells = Array.prototype.slice.call(grid.querySelectorAll('.sb-skill-know-cell'));
            var cellTop = Math.round(cell.getBoundingClientRect().top);
            var rowEndCell = cell;
            cells.forEach(function (candidate) {
                if (Math.abs(Math.round(candidate.getBoundingClientRect().top) - cellTop) <= 6) {
                    rowEndCell = candidate;
                }
            });
            return rowEndCell;
        }

        function refreshDrawerStageHighlights(skillId) {
            if (!activeDrawerEl || activeDrawerSkillId !== skillId) return;
            var currentStage = selectedKnow[skillId] || 1;
            var stages = skillStagesData[skillId] || [];
            var statusLabel = activeDrawerEl.querySelector('[data-sb-drawer-status]');
            if (statusLabel) {
                statusLabel.textContent = formatStageSummary(currentStage, Math.max(1, stages.length));
            }
            activeDrawerEl.querySelectorAll('.stage-option').forEach(function (opt) {
                var stNum = parseInt(opt.getAttribute('data-stage') || '1', 10);
                var selected = stNum <= currentStage;
                opt.classList.toggle('is-selected', selected);
                opt.setAttribute('aria-pressed', selected ? 'true' : 'false');
            });
        }

        function openStageDrawer(skillId, focusFirst) {
            var stages = skillStagesData[skillId];
            if (!stages || !stages.length) return;
            var cell = board.querySelector('.sb-skill-know-cell[data-skill-id="' + skillId + '"]');
            if (!cell) return;
            var grid = cell.closest('.sb-skill-know-grid');
            if (!grid) return;

            var btn = cell.querySelector('.skill-know');
            var skillName = btn ? (btn.getAttribute('data-skill-name') || 'Skill') : 'Skill';
            var currentStage = selectedKnow[skillId] || 1;

            closeStageDrawer(false);

            var drawer = document.createElement('div');
            drawer.className = 'sb-stage-drawer stage-selection-container';
            drawer.id = 'stage-selection-' + skillId;
            drawer.setAttribute('role', 'region');
            drawer.setAttribute('aria-label', 'Stages you can teach for ' + skillName);

            var header = document.createElement('div');
            header.className = 'sb-stage-drawer-header';

            var titleWrap = document.createElement('div');
            titleWrap.className = 'sb-stage-drawer-title-wrap';
            var title = document.createElement('strong');
            title.className = 'sb-stage-drawer-title';
            title.textContent = skillName + ': highest stage you can teach';
            titleWrap.appendChild(title);

            var statusPill = document.createElement('span');
            statusPill.className = 'sb-stage-drawer-status';
            statusPill.setAttribute('data-sb-drawer-status', '');
            statusPill.textContent = formatStageSummary(currentStage, stages.length);
            titleWrap.appendChild(statusPill);
            header.appendChild(titleWrap);

            var actions = document.createElement('div');
            actions.className = 'sb-stage-drawer-actions';

            var doneBtn = document.createElement('button');
            doneBtn.type = 'button';
            doneBtn.className = 'sb-drawer-action-btn sb-drawer-done-btn';
            doneBtn.textContent = 'Done';
            doneBtn.addEventListener('click', function () {
                closeStageDrawer(true);
            });
            actions.appendChild(doneBtn);

            var removeBtn = document.createElement('button');
            removeBtn.type = 'button';
            removeBtn.className = 'sb-drawer-action-btn sb-drawer-remove-btn';
            removeBtn.textContent = 'Deselect';
            removeBtn.addEventListener('click', function () {
                deselectKnowSkill(skillId, true);
            });
            actions.appendChild(removeBtn);

            header.appendChild(actions);
            drawer.appendChild(header);

            var hint = document.createElement('p');
            hint.className = 'sb-stage-drawer-hint';
            hint.textContent = 'Selecting a stage marks stages 1 through that level in blue.';
            drawer.appendChild(hint);

            var list = document.createElement('div');
            list.className = 'sb-stage-options-grid';

            stages.forEach(function (st) {
                var opt = document.createElement('button');
                opt.type = 'button';
                opt.className = 'sb-stage-option stage-option' + (st.id <= currentStage ? ' is-selected' : '');
                opt.setAttribute('data-stage', String(st.id));
                opt.setAttribute('data-skill-id', skillId);
                opt.setAttribute('aria-pressed', st.id <= currentStage ? 'true' : 'false');

                var numSpan = document.createElement('span');
                numSpan.className = 'sb-stage-num';
                numSpan.textContent = String(st.id);
                opt.appendChild(numSpan);

                var textSpan = document.createElement('span');
                textSpan.className = 'sb-stage-name';
                textSpan.textContent = st.name;
                opt.appendChild(textSpan);

                opt.addEventListener('click', function () {
                    selectedKnow[skillId] = st.id;
                    syncHiddenInputs();
                    updateKnowCardVisuals(skillId);
                    refreshDrawerStageHighlights(skillId);
                });

                list.appendChild(opt);
            });

            drawer.appendChild(list);

            var anchorCell = findRowAnchorCell(cell, grid);
            if (anchorCell.nextSibling) {
                grid.insertBefore(drawer, anchorCell.nextSibling);
            } else {
                grid.appendChild(drawer);
            }

            activeDrawerSkillId = skillId;
            activeDrawerEl = drawer;
            updateKnowCardVisuals(skillId);

            if (focusFirst) {
                var firstSelected = drawer.querySelector('.stage-option.is-selected') || drawer.querySelector('.stage-option');
                if (firstSelected) firstSelected.focus();
            }
        }

        function deselectKnowSkill(skillId, focusSkillBtn) {
            delete selectedKnow[skillId];
            if (activeDrawerSkillId === skillId) {
                closeStageDrawer(false);
            }
            syncHiddenInputs();
            updateKnowCardVisuals(skillId);
            updateCounters();
            if (focusSkillBtn) {
                var btn = board.querySelector('.sb-skill-know-cell[data-skill-id="' + skillId + '"] .skill-know');
                if (btn) btn.focus();
            }
        }

        board.addEventListener('click', function (event) {
            var learnBtn = event.target.closest('.skill-learn');
            if (learnBtn && board.contains(learnBtn)) {
                var learnId = String(learnBtn.getAttribute('data-skill-id'));
                var nowActive = !selectedLearn.has(learnId);
                if (nowActive) selectedLearn.add(learnId);
                else selectedLearn.delete(learnId);

                learnBtn.classList.toggle('active', nowActive);
                learnBtn.classList.toggle('is-selected', nowActive);
                learnBtn.setAttribute('aria-pressed', nowActive ? 'true' : 'false');
                syncHiddenInputs();
                updateCounters();
                return;
            }

            var stageTrigger = event.target.closest('.sb-stage-trigger');
            if (stageTrigger && board.contains(stageTrigger)) {
                var triggerSkillId = String(stageTrigger.getAttribute('data-skill-id'));
                if (activeDrawerSkillId === triggerSkillId) {
                    closeStageDrawer(false);
                } else {
                    openStageDrawer(triggerSkillId, true);
                }
                return;
            }

            var knowBtn = event.target.closest('.skill-know');
            if (knowBtn && board.contains(knowBtn)) {
                var knowId = String(knowBtn.getAttribute('data-skill-id'));
                var isSelected = Object.prototype.hasOwnProperty.call(selectedKnow, knowId);
                if (isSelected) {
                    deselectKnowSkill(knowId, false);
                } else {
                    var totalStages = (skillStagesData[knowId] && skillStagesData[knowId].length) ? skillStagesData[knowId].length : 1;
                    var prefill = verifiedLearningStages[knowId] || 1;
                    selectedKnow[knowId] = Math.min(totalStages, Math.max(1, prefill));
                    syncHiddenInputs();
                    updateKnowCardVisuals(knowId);
                    updateCounters();
                    openStageDrawer(knowId, false);
                }
            }
        });

        board.addEventListener('keydown', function (event) {
            if (event.key === 'Escape' && activeDrawerSkillId) {
                event.preventDefault();
                closeStageDrawer(true);
            }
        });

        var resizeTimer = null;
        window.addEventListener('resize', function () {
            if (!activeDrawerSkillId || !activeDrawerEl) return;
            clearTimeout(resizeTimer);
            resizeTimer = setTimeout(function () {
                if (!activeDrawerSkillId || !activeDrawerEl) return;
                var cell = board.querySelector('.sb-skill-know-cell[data-skill-id="' + activeDrawerSkillId + '"]');
                var grid = cell ? cell.closest('.sb-skill-know-grid') : null;
                if (!cell || !grid) return;
                var anchorCell = findRowAnchorCell(cell, grid);
                if (anchorCell.nextSibling !== activeDrawerEl) {
                    if (anchorCell.nextSibling) grid.insertBefore(activeDrawerEl, anchorCell.nextSibling);
                    else grid.appendChild(activeDrawerEl);
                }
            }, 90);
        });

        updateCounters();
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-sb-avatar-picker]').forEach(initAvatarPicker);
        document.querySelectorAll('[data-sb-skill-board]').forEach(initSkillBoard);
    });
})();
