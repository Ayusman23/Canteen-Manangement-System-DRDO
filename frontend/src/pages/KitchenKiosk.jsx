import { useState, useEffect, useCallback } from 'react';
import { Html5QrcodeScanner } from 'html5-qrcode';
import { toast } from 'sonner';
import { motion, AnimatePresence } from 'framer-motion';
import api from '../services/api';
import { useAuth } from '../context/AuthContext';
import {
  QrCode,
  CheckCircle2,
  AlertTriangle,
  Search,
  Camera,
  ChefHat,
} from 'lucide-react';

export default function KitchenKiosk() {
  const { user } = useAuth();
  const [manualToken, setManualToken] = useState('');
  const [isProcessing, setIsProcessing] = useState(false);
  const [dispenseResult, setDispenseResult] = useState(null);
  const [recentDispensed, setRecentDispensed] = useState([]);
  const [servedCount, setServedCount] = useState(0);
  const [scannerActive, setScannerActive] = useState(true);

  const processTokenDispense = useCallback(async (tokenPayload) => {
    setIsProcessing(true);
    setDispenseResult(null);

    try {
      const res = await api.post('/bookings/dispense', {
        token: tokenPayload,
        dispensedBy: user?.fullName || 'Kitchen Operator',
        notes: 'Dispensed at Kiosk Terminal',
      });

      const data = res.data;
      setDispenseResult({
        success: true,
        message: data.message,
        userName: data.userName,
        mealName: data.mealName,
        bookingReference: data.bookingReference,
        dispensedAt: new Date(data.dispensedAt).toLocaleTimeString(),
      });

      setServedCount((prev) => prev + 1);
      setRecentDispensed((prev) => [
        {
          id: Date.now(),
          token: data.bookingReference,
          name: data.userName,
          meal: data.mealName,
          time: new Date(data.dispensedAt).toLocaleTimeString(),
        },
        ...prev.slice(0, 9),
      ]);

      toast.success(`Dispensed: ${data.mealName} for ${data.userName}`);
      setManualToken('');
    } catch (err) {
      const errMsg = err.response?.data?.detail || err.response?.data?.message || 'Token verification failed.';
      setDispenseResult({
        success: false,
        message: errMsg,
      });
      toast.error(errMsg);
    } finally {
      setIsProcessing(false);
    }
  }, [user?.fullName]);

  // Initialize camera scanner
  useEffect(() => {
    let html5QrcodeScanner = null;

    if (scannerActive) {
      const timer = setTimeout(() => {
        try {
          html5QrcodeScanner = new Html5QrcodeScanner(
            'reader',
            {
              fps: 10,
              qrbox: { width: 250, height: 250 },
              rememberLastUsedCamera: true,
              aspectRatio: 1.0,
            },
            false
          );

          html5QrcodeScanner.render(
            (decodedText) => {
              if (!isProcessing) {
                processTokenDispense(decodedText);
              }
            },
            (_err) => {
              // Ignore standard frame scan errors
            }
          );
        } catch (err) {
          console.warn('QR Scanner init warning:', err);
        }
      }, 300);

      return () => {
        clearTimeout(timer);
        if (html5QrcodeScanner) {
          html5QrcodeScanner.clear().catch(console.error);
        }
      };
    }
  }, [scannerActive, isProcessing, processTokenDispense]);

  const handleManualSubmit = (e) => {
    e.preventDefault();
    if (!manualToken.trim()) return;
    processTokenDispense(manualToken.trim());
  };

  return (
    <div className="kitchen-kiosk-layout">
      {/* Top Banner */}
      <div className="kiosk-header">
        <div className="kiosk-title-area">
          <div className="chef-icon-box">
            <ChefHat size={28} />
          </div>
          <div>
            <h2>Kitchen Dispensing Terminal</h2>
            <p>Rapid QR Token Redemption • Anti-Duplicate Safeguard Active</p>
          </div>
        </div>

        <div className="kiosk-stats">
          <div className="kiosk-stat-pill">
            <span className="pill-label">Meals Served Today</span>
            <span className="pill-number">{servedCount}</span>
          </div>
          <div className="operator-badge">
            <span className="op-label">Operator:</span>
            <span className="op-name">{user?.fullName}</span>
          </div>
        </div>
      </div>

      <div className="kiosk-grid">
        {/* Left Column: Scanner & Manual Input */}
        <div className="kiosk-card scanner-section">
          <div className="scanner-tabs">
            <button
              className={`tab-btn ${scannerActive ? 'active' : ''}`}
              onClick={() => setScannerActive(true)}
            >
              <Camera size={16} /> Live Camera Scanner
            </button>
            <button
              className={`tab-btn ${!scannerActive ? 'active' : ''}`}
              onClick={() => setScannerActive(false)}
            >
              <Search size={16} /> Manual Input Only
            </button>
          </div>

          {scannerActive && (
            <div className="camera-viewfinder-box">
              <div id="reader" className="html5-qrcode-reader"></div>
              <p className="scanner-instruction">Hold employee token QR code steady in the viewfinder</p>
            </div>
          )}

          {/* Manual Token Form */}
          <div className="manual-input-box">
            <h4>Manual Token Lookup & Dispense</h4>
            <form onSubmit={handleManualSubmit} className="manual-form">
              <input
                type="text"
                placeholder="Enter token (e.g. DRDO-20260911-7788)"
                value={manualToken}
                onChange={(e) => setManualToken(e.target.value)}
                disabled={isProcessing}
              />
              <button type="submit" className="btn-primary" disabled={isProcessing || !manualToken.trim()}>
                {isProcessing ? 'Verifying...' : 'Redeem Token'}
              </button>
            </form>
          </div>
        </div>

        {/* Right Column: Validation Feedback & Recent Log */}
        <div className="kiosk-card feedback-section">
          <h3>Validation Result</h3>

          <AnimatePresence mode="wait">
            {isProcessing ? (
              <div className="feedback-placeholder">
                <div className="spinner"></div>
                <p>Verifying clearance and checking token integrity...</p>
              </div>
            ) : dispenseResult ? (
              <motion.div
                key={dispenseResult.success ? 'success' : 'error'}
                className={`validation-banner ${dispenseResult.success ? 'success' : 'error'}`}
                initial={{ opacity: 0, scale: 0.95 }}
                animate={{ opacity: 1, scale: 1 }}
                exit={{ opacity: 0 }}
              >
                {dispenseResult.success ? (
                  <>
                    <CheckCircle2 size={48} className="result-icon" />
                    <h4>CLEARANCE CONFIRMED</h4>
                    <p className="result-headline">{dispenseResult.message}</p>
                    <div className="result-details">
                      <div>
                        <span>Recipient</span>
                        <strong>{dispenseResult.userName}</strong>
                      </div>
                      <div>
                        <span>Meal</span>
                        <strong>{dispenseResult.mealName}</strong>
                      </div>
                      <div>
                        <span>Token Ref</span>
                        <strong>{dispenseResult.bookingReference}</strong>
                      </div>
                      <div>
                        <span>Timestamp</span>
                        <strong>{dispenseResult.dispensedAt}</strong>
                      </div>
                    </div>
                  </>
                ) : (
                  <>
                    <AlertTriangle size={48} className="result-icon error-icon" />
                    <h4>DISPENSE REJECTED</h4>
                    <p className="result-headline">{dispenseResult.message}</p>
                    <div className="security-alert-box">
                      Token not honored. Possible duplication, cancellation, or unknown reference.
                    </div>
                  </>
                )}
              </motion.div>
            ) : (
              <div className="feedback-placeholder">
                <QrCode size={40} color="var(--text-muted)" />
                <p>Waiting for token scan or manual reference input...</p>
              </div>
            )}
          </AnimatePresence>

          {/* Recent Redemptions Table */}
          <div className="recent-queue-box">
            <h4>Live Dispensed Activity Log</h4>
            {recentDispensed.length === 0 ? (
              <p className="empty-log">No tokens dispensed yet during this terminal session.</p>
            ) : (
              <div className="recent-queue-list">
                {recentDispensed.map((item) => (
                  <div key={item.id} className="queue-row">
                    <span className="queue-token">{item.token}</span>
                    <span className="queue-name">{item.name}</span>
                    <span className="queue-meal">{item.meal}</span>
                    <span className="queue-time">{item.time}</span>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
