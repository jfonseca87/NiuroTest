"use client";

import Link from "next/link";
import { Menubar } from "primereact/menubar";
import type { MenuItem } from "primereact/menuitem";

export function Navbar() {
  const items: MenuItem[] = [
    {
      label: "Home",
      icon: "pi pi-home",
      command: () => {},
    },
  ];

  const brand = (
    <Link href="/" className="navbar-brand text-decoration-none fw-bold">
      <i className="pi pi-building me-2"></i>
      NiuroTest
    </Link>
  );

  return (
    <nav className="navbar navbar-expand-lg navbar-dark bg-primary shadow-sm">
      <div className="container">
        <Menubar
          model={items}
          start={brand}
          className="bg-transparent border-0"
        />
      </div>
    </nav>
  );
}
