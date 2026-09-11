import { Navigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { ShieldAlert } from 'lucide-react';

export default function ProtectedRoute({ children, allowedRoles }) {
  const { user, isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div className="flex-center" style={{ minHeight: '60vh' }}>
        <div className="spinner"></div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (allowedRoles && !allowedRoles.includes(user?.role)) {
    return (
      <div className="container" style={{ padding: '80px 20px', textAlign: 'center' }}>
        <div style={{ maxWidth: '480px', margin: '0 auto', background: 'var(--surface)', padding: '40px', borderRadius: '16px', border: '1px solid var(--border)' }}>
          <ShieldAlert size={48} color="var(--danger)" style={{ marginBottom: '16px' }} />
          <h2>Security Clearance Restricted</h2>
          <p style={{ color: 'var(--text-secondary)', margin: '16px 0 24px' }}>
            Your current security role (<strong>{user?.role}</strong>) does not have access to this defense partition.
          </p>
          <a href="/" className="btn-primary">Return to Portal</a>
        </div>
      </div>
    );
  }

  return children;
}
