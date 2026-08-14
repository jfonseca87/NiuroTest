"use client";

import Link from "next/link";
import { Menubar } from "primereact/menubar";
import type { MenuItem } from "primereact/menuitem";

export function Navbar() {
  const items: MenuItem[] = [
    {
      label: "Home",
      icon: "pi pi-home",
      url: "/",
      command: () => {},
    },
  ];

  const brand = (
    <Link
      href="/"
      className="navbar-brand text-decoration-none fw-bold d-flex align-items-center gap-2"
      style={{ color: "var(--text-primary)" }}
    >
      <span className="icon-badge" style={{ width: "2.5rem", height: "2.5rem" }}>
        <i className="pi pi-building"></i>
      </span>
      <span>
        Niuro<span className="gradient-text">Test</span>
      </span>
    </Link>
  );

  return (
    <nav
      className="navbar navbar-expand-lg sticky-top glass py-3"
      aria-label="Primary navigation"
    >
      <div className="container">
        <Menubar
          model={items}
          start={brand}
          className="bg-transparent border-0 w-100 justify-content-between"
        />
      </div>
    </nav>
  );
}
