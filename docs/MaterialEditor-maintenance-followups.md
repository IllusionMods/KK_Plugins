# Material Editor: follow-ups after 2.5/7

Keep each change small and readable. Prefer direct code and local helpers over
new managers, registries, or one-method files. Preserve public APIs, save keys,
MessagePack layouts, and existing user-visible behavior unless fixing a bug.

## 2.6/7 scope

- Fix `OnCardBeingSaved`: include material names and projector edits in the empty
  check. Previously, cards containing only either kind lost those records.
- Restore local texture writes in `TextureSaveHandler.Save`: honor Maker/Studio
  local mode, write external files and version-2 `LOCAL_` hash references, and do
  not embed texture bytes or silently fall back to bundled saving on I/O failure.
- Keep bundled writes and bundled/local/deduplicated readers compatible. This is
  not a restoration of scene deduplicated writing or KKAPI audit registration.
  Coordinate texture saving remains unchanged.

## Recorded findings 2–8

2. **Save-list mismatch — bug, character-card fix in 2.6/7.**
   `MaterialEditorCharaController.OnCardBeingSaved` omitted
   `MaterialNamePropertyList` and `ProjectorPropertyList` from its empty check.
   Coordinate saving also omits projectors in its empty check, but coordinate
   loading does not restore them either. Decide coordinate support separately;
   changing just that predicate would not implement a complete feature.

3. **Temporary graphics resources — exception-path bug, deferred.**
   `PluginBase.GetT2D`, `SaveTexR`, and `SaveTex` need exception-safe cleanup.
   Restore `RenderTexture.active` and release/destroy owned temporary resources
   in `finally` blocks. Preserve ownership of caller-supplied textures.

4. **Invalidation bookkeeping — cleanup, deferred.**
   `UI.PresentationInvalidationCoordinator.cs` carries unused version fields,
   batch version data, and accessors. Remove only values with no consumers;
   retain the generation and worker-lease checks that invalidate stale work.

5. **Invalidation flush duplication — cleanup, deferred.**
   `TryBeginFlush` and `TryBeginRecoveryFlush` repeat batch construction and
   pending-state clearing. `PresentationInvalidationWorker` and
   `RecoverPresentationInvalidationWorkerStart` repeat apply/error/completion
   handling. Share local steps without merging their different scheduling gates.

6. **Deferred populate without a worker — failure-path bug, deferred.**
   `MaterialEditorRefreshController.ScheduleDeferredPopulate` handles a null
   coroutine by stopping the worker, but can leave a pending request with no
   worker to complete it. Ensure waiters finish or receive a terminal outcome;
   do not introduce an unbounded automatic retry.

7. **Dropdown caption refresh — redundant work, deferred.**
   `MaterialEditorDropdownCaptionFitter.Refresh` calls `ConfigureInternal`, which
   refreshes text, then refreshes it again with the supplied caption. Bind only
   when the target changes and measure the intended caption once per refresh.

8. **Theme/repaint text passes — optimization candidate, deferred.**
   `UI.WindowView` reapplies the theme (including a text pass), then requests a
   repaint with another immediate text pass and two deferred passes. Check the
   duplicate immediate work first. Retain delayed passes needed for pooled rows
   and ColorTint updates until runtime verification demonstrates redundancy.

After implementation, validate the affected paths before publishing each PR.
Do not use these findings as a reason for another broad file-splitting refactor.
