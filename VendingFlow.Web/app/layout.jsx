import './globals.css';

export const metadata = {
  title: 'VendingFlow',
  description: 'Distributed vending fleet operations'
};

export default function RootLayout({ children }) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
