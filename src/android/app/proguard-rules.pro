# ProGuard rules for ProjectName Android
# Keep accessibility and media projection services
-keep class com.projectname.android.accessibility.ProjectNameAccessibilityService { *; }
-keep class com.projectname.android.capture.MediaProjectionCaptureService { *; }
