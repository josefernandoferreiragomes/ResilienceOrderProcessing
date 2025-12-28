# 🎨 Order Processing Dashboard - Features Guide

## Overview
The enhanced `LoggingDashboard.html` provides a real-time monitoring interface for the Order Processing API with SignalR integration, live statistics, and feature toggle controls.

## ✨ Key Features

### 1. **Real-time Log Streaming** 📋
- **Live Updates**: Logs appear instantly as events occur
- **Reverse Chronological**: Newest logs appear at the top
- **Color-coded Severity**: 
  - 🔵 Information (Blue)
  - 🟠 Warning (Orange)
  - 🔴 Error (Red)
- **Request Correlation**: Display unique RequestId for tracing
- **Log Properties**: Show structured metadata from operations

### 2. **Intelligent Filtering** 🔍
- **By Severity Level**: Toggle Information, Warning, Error
- **By Category**: Filter for Request, Order, Performance logs
- **Multi-select**: Combine filters for precise views
- **Real-time Application**: Filters update instantly without page reload

### 3. **Live Statistics** 📊
- **Information Count**: Total informational logs
- **Warning Count**: Number of warnings
- **Error Count**: Exception and error tracking
- **Total Logs**: Complete log count (max 500)
- **Auto-update**: Stats refresh as logs arrive

### 4. **Feature Toggle Controls** 🎚️
- **RealTimeLogging**: Enable/disable SignalR broadcasting
- **DetailedErrorLogging**: Toggle stack trace inclusion
- **PerformanceLogging**: Control operation timing metrics
- **Visual Indicators**: Toggle switches show active state
- **API Integration**: Changes persist to backend

### 5. **Connection Management** 🔗
- **Status Indicator**: 
  - 🟢 Connected (green, solid)
  - 🔴 Disconnected (red, pulsing)
- **Auto-Reconnect**: Attempts 5 reconnection with backoff
- **Status Messages**: Clear connection state information
- **Manual Control**: Connect/Disconnect buttons

### 6. **Data Management** 💾
- **Clear Logs**: Remove all logs with confirmation
- **Export CSV**: Download logs as spreadsheet
  - Includes timestamp, level, category, message
  - Compatible with Excel, Google Sheets
  - Timestamped filename for organization
- **Max Log Retention**: 500 logs (auto-cleanup)

### 7. **Modern UI/UX** 🎨
- **Glassmorphism Design**: Frosted glass effect with backdrop blur
- **Responsive Layout**: 
  - Desktop: Main logs + sidebar stats
  - Tablet: Stacked layout with full width logs
  - Mobile: Single column with touch-friendly controls
- **Smooth Animations**: Slide-in effects for new logs
- **Dark Mode Friendly**: Light background with good contrast

## 🖥️ Layout

```
┌─────────────────────────────────────────┬─────────────────┐
│                                         │                 │
│           HEADER (Title/Controls)       │                 │
│                                         │                 │
├─────────────────────────────────────────┤   STATISTICS    │
│                                         │   ┌─────────┐   │
│                                         │   │ Info: 0 │   │
│         FILTERS (Levels/Categories)     │   │ Warn: 0 │   │
│                                         │   │ Errs: 0 │   │
├─────────────────────────────────────────┤   │ Total:0 │   │
│                                         │   └─────────┘   │
│                                         │                 │
│          REAL-TIME LOGS STREAM          │  FEATURE        │
│     (Auto-scrolling, Color-coded)       │  TOGGLES        │
│                                         │  ┌────┐         │
│                                         │  │RealTime│      │
│                                         │  │Detailed│      │
│                                         │  │Performa│      │
│                                         │  └────┘         │
│                                         │                 │
│                                         │  INFO           │
│                                         │  API URLs       │
│                                         │  Descriptions   │
└─────────────────────────────────────────┴─────────────────┘
```

## 🚀 Usage Instructions

### Connecting to LoggingApi
1. Click the **🔗 Connect** button
2. Dashboard shows "Connected" with green indicator
3. Logs begin streaming in real-time

### Filtering Logs
1. Use **Log Levels** filters:
   - ✓ Information (checked by default)
   - ✓ Warning (checked by default)
   - ✓ Error (checked by default)

2. Use **Category** filters:
   - ✓ Request (HTTP request/response)
   - ✓ Order (business logic)
   - ✓ Performance (timing metrics)

3. Uncheck items to hide, recheck to show

### Managing Feature Toggles
1. Click any toggle switch to enable/disable
2. Slider moves left/right to indicate state
3. Changes sent to LoggingApi backend
4. Affects real-time logging behavior

### Exporting Logs
1. Click **📥 Export** button
2. CSV file downloads automatically
3. Opens in Excel or Google Sheets
4. Filename includes timestamp

### Clearing Logs
1. Click **🗑️ Clear Logs** button
2. Confirmation dialog appears
3. All logs cleared, stats reset
4. Fresh start for new monitoring session

## 📡 SignalR Integration

### Events Received
- **ReceiveLog**: Standard log messages
  - Properties: level, message, source, category, timestamp, properties
  
- **ReceivePerformanceLog**: Performance metrics
  - Properties: operation, duration, source, metadata
  
- **FeatureToggleUpdated**: Feature state changes
  - Properties: featureName, isEnabled

### Auto-Reconnection
- Attempts: 5 retries
- Backoff Strategy: 0ms, 0ms, 0ms, 1000ms, 3000ms, 5000ms
- Status Display: "Reconnecting..." message during attempts

## 🎯 Common Workflows

### Monitor Order Creation
1. Connect to dashboard
2. Filter: Order category only
3. Create order via API
4. Watch logs stream with RequestId correlation
5. Export logs for record keeping

### Debug Performance Issues
1. Connect and enable PerformanceLogging toggle
2. Filter: Performance category
3. Trigger operations
4. View operation durations
5. Identify bottlenecks

### Track Resilience Events
1. Filter: All categories, Error level only
2. Observe: Circuit breaker events, retry attempts
3. Monitor: Retry count and timing
4. Export: For analytics

### Test Feature Flags
1. Create/cancel orders with toggles OFF
2. Toggle ON in dashboard
3. Repeat same operations
4. Compare log output differences
5. Verify feature behavior

## 🔧 Customization

### Changing Theme Colors
Edit CSS gradient in `<style>`:
```css
background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
```
Change hex colors to match your brand.

### Adjusting Max Logs
Modify in JavaScript:
```javascript
const maxLogs = 500; // Change to desired limit
```

### Custom Log Categories
Add new categories in filter section:
```html
<label class="filter-checkbox">
    <input type="checkbox" id="filterCustom" checked onchange="applyFilters()">
    Custom Category
</label>
```

### Additional Feature Toggles
Add more toggles in HTML:
```html
<div class="toggle-item">
    <span class="toggle-name">NewFeature</span>
    <button class="toggle-switch" onclick="toggleFeature('NewFeature', this)">
        <div class="toggle-slider"></div>
    </button>
</div>
```

## 📱 Responsive Breakpoints

- **Desktop (1024px+)**: Two-column layout with sidebar
- **Tablet (768-1024px)**: Single column with adjusted stats grid
- **Mobile (< 768px)**: Full width logs, stacked controls

## ⚙️ Configuration

### API Endpoints
```javascript
LoggingApi: 'https://localhost:7002'
SignalR Hub: 'https://localhost:7002/loggingHub'
Feature Toggle: 'https://localhost:7002/api/featuretoggle'
```

### SignalR Connection Options
```javascript
skipNegotiation: true
transport: WebSockets
autoReconnect: 5 attempts with backoff
```

## 🐛 Troubleshooting

### Logs Not Appearing
- Verify LoggingApi is running (`https://localhost:7002`)
- Check browser console for connection errors
- Ensure WebSocket support in your network
- Try reconnecting with 🔗 Connect button

### Feature Toggles Not Working
- Confirm LoggingApi is accessible
- Check network tab for 404/500 errors
- Verify toggle names match backend (case-sensitive)

### Styling Issues
- Clear browser cache (Ctrl+Shift+Del)
- Ensure no CSS overrides in page
- Check browser compatibility (Chrome/Firefox recommended)

## 📊 Performance Notes

- Dashboard handles 500 logs smoothly
- Filters applied client-side (instant)
- New logs added with CSS animation
- Export creates CSV in-memory
- WebSocket connection for real-time updates

## 🔐 Security Considerations

- Dashboard uses HTTPS for API calls
- No sensitive data logged by default
- Toggle feature flags to control verbosity
- Clear logs before sharing sessions
- Consider authentication for production

## 📞 Support

For issues or enhancements:
1. Check browser console (F12)
2. Verify all services are running
3. Test with simpler API calls first
4. Review README for setup instructions
